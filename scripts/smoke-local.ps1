[CmdletBinding()]
param(
    [string]$BaseUrl = $env:INTRANET_SMOKE_BASE_URL
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
    $BaseUrl = "http://127.0.0.1:5077"
}

$BaseUrl = $BaseUrl.TrimEnd("/")
$BaseUri = [Uri]$BaseUrl
$fallas = New-Object System.Collections.Generic.List[string]

Add-Type -AssemblyName System.Net.Http

function New-SmokeClient {
    param([bool]$AllowRedirect = $false)

    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $AllowRedirect
    $handler.CookieContainer = [System.Net.CookieContainer]::new()

    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.BaseAddress = $BaseUri

    [pscustomobject]@{
        Client = $client
        Handler = $handler
    }
}

function Write-SmokeResult {
    param(
        [string]$Name,
        [bool]$Ok,
        [string]$Detail = ""
    )

    if ($Ok) {
        Write-Host ("[OK]   {0} {1}" -f $Name, $Detail).TrimEnd() -ForegroundColor Green
        return
    }

    Write-Host ("[FAIL] {0} {1}" -f $Name, $Detail).TrimEnd() -ForegroundColor Red
    $script:fallas.Add(("{0} {1}" -f $Name, $Detail).TrimEnd())
}

function Invoke-SmokeGet {
    param(
        [System.Net.Http.HttpClient]$Client,
        [string]$Path
    )

    $Client.GetAsync($Path).GetAwaiter().GetResult()
}

function New-FormContent {
    param([hashtable]$Values)

    $encoded = $Values.GetEnumerator() | ForEach-Object {
        [Uri]::EscapeDataString([string]$_.Key) + "=" + [Uri]::EscapeDataString([string]$_.Value)
    }

    [System.Net.Http.StringContent]::new(
        ($encoded -join "&"),
        [Text.Encoding]::UTF8,
        "application/x-www-form-urlencoded")
}

function Get-AntiForgeryToken {
    param([string]$Html)

    $match = [regex]::Match(
        $Html,
        'name="__RequestVerificationToken"[^>]*value="([^"]+)"',
        [Text.RegularExpressions.RegexOptions]::IgnoreCase)

    if (-not $match.Success) {
        $match = [regex]::Match(
            $Html,
            'value="([^"]+)"[^>]*name="__RequestVerificationToken"',
            [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    }

    if (-not $match.Success) {
        throw "No se encontro token antiforgery en /Admin/Login."
    }

    [System.Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
}

Write-Host "Smoke test local: $BaseUrl" -ForegroundColor Cyan

$publicClientBundle = New-SmokeClient
$publicClient = $publicClientBundle.Client

$publicRoutes = @(
    @{ Path = "/health/live"; Expected = 200 },
    @{ Path = "/health/ready"; Expected = 200 },
    @{ Path = "/"; Expected = 200 },
    @{ Path = "/Directorio"; Expected = 200 },
    @{ Path = "/Formatos"; Expected = 200 },
    @{ Path = "/Manuales"; Expected = 200 },
    @{ Path = "/Dgie"; Expected = 200 },
    @{ Path = "/Identidad"; Expected = 200 },
    @{ Path = "/Capacitacion"; Expected = 200 },
    @{ Path = "/Tutoriales"; Expected = 200 },
    @{ Path = "/Admin/Login"; Expected = 200 }
)

foreach ($route in $publicRoutes) {
    try {
        $response = Invoke-SmokeGet -Client $publicClient -Path $route.Path
        Write-SmokeResult -Name $route.Path -Ok ([int]$response.StatusCode -eq $route.Expected) -Detail ("status={0}" -f [int]$response.StatusCode)
        $response.Dispose()
    }
    catch {
        Write-SmokeResult -Name $route.Path -Ok $false -Detail $_.Exception.Message
    }
}

try {
    $anonResponse = Invoke-SmokeGet -Client $publicClient -Path "/Admin"
    $location = if ($anonResponse.Headers.Location) { $anonResponse.Headers.Location.ToString() } else { "" }
    $ok = [int]$anonResponse.StatusCode -eq 302 -and $location -match "/Admin/Login"
    Write-SmokeResult -Name "/Admin sin sesion" -Ok $ok -Detail ("status={0} location={1}" -f [int]$anonResponse.StatusCode, $location)
    $anonResponse.Dispose()
}
catch {
    Write-SmokeResult -Name "/Admin sin sesion" -Ok $false -Detail $_.Exception.Message
}

$adminUser = $env:INTRANET_SMOKE_ADMIN_USER
$adminPassword = $env:INTRANET_SMOKE_ADMIN_PASSWORD

if ([string]::IsNullOrWhiteSpace($adminUser) -or [string]::IsNullOrWhiteSpace($adminPassword)) {
    Write-Host "[SKIP] Rutas admin autenticadas: faltan INTRANET_SMOKE_ADMIN_USER/INTRANET_SMOKE_ADMIN_PASSWORD." -ForegroundColor Yellow
}
else {
    $adminClientBundle = New-SmokeClient
    $adminClient = $adminClientBundle.Client

    try {
        $loginPage = Invoke-SmokeGet -Client $adminClient -Path "/Admin/Login"
        $loginHtml = $loginPage.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $loginPage.Dispose()

        $token = Get-AntiForgeryToken -Html $loginHtml
        $loginContent = New-FormContent @{
            "__RequestVerificationToken" = $token
            "Usuario" = $adminUser
            "Password" = $adminPassword
            "ReturnUrl" = ""
        }

        $loginResponse = $adminClient.PostAsync("/Admin/Login", $loginContent).GetAwaiter().GetResult()
        $loginLocation = if ($loginResponse.Headers.Location) { $loginResponse.Headers.Location.ToString() } else { "" }
        $loginOk = [int]$loginResponse.StatusCode -eq 302 -and $loginLocation -match "/Admin"
        Write-SmokeResult -Name "login admin" -Ok $loginOk -Detail ("status={0}" -f [int]$loginResponse.StatusCode)
        $loginResponse.Dispose()

        if ($loginOk) {
            $adminRoutes = @(
                "/Admin",
                "/Admin/Configuracion",
                "/Admin/Directorio",
                "/Admin/AccesosRapidos",
                "/Admin/CambiarPassword"
            )

            foreach ($route in $adminRoutes) {
                try {
                    $response = Invoke-SmokeGet -Client $adminClient -Path $route
                    Write-SmokeResult -Name $route -Ok ([int]$response.StatusCode -eq 200) -Detail ("status={0}" -f [int]$response.StatusCode)
                    $response.Dispose()
                }
                catch {
                    Write-SmokeResult -Name $route -Ok $false -Detail $_.Exception.Message
                }
            }
        }
    }
    catch {
        Write-SmokeResult -Name "login admin" -Ok $false -Detail $_.Exception.Message
    }
    finally {
        $adminClient.Dispose()
        $adminClientBundle.Handler.Dispose()
    }
}

$publicClient.Dispose()
$publicClientBundle.Handler.Dispose()

if ($fallas.Count -gt 0) {
    Write-Host ""
    Write-Host ("Smoke test FALLIDO: {0} falla(s)." -f $fallas.Count) -ForegroundColor Red
    foreach ($falla in $fallas) {
        Write-Host (" - {0}" -f $falla) -ForegroundColor Red
    }
    exit 1
}

Write-Host ""
Write-Host "Smoke test OK." -ForegroundColor Green
exit 0
