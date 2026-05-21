[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Web

$BaseUrl = "http://localhost:5099"
$ImagePath = "d:\test\t\wwwroot\img\hero-1.jpg"
$script:Results = @()

function Get-AntiforgeryToken {
    param([string]$Html)
    $patterns = @(
        'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"'
        'type="hidden"\s+name="__RequestVerificationToken"\s+value="([^"]+)"'
        '__RequestVerificationToken"[^>]+value="([^"]+)"'
    )
    foreach ($p in $patterns) {
        $m = [regex]::Match($Html, $p)
        if ($m.Success) { return $m.Groups[1].Value }
    }
    return $null
}

function Get-Page {
    param($Session, [string]$Path)
    try {
        $r = Invoke-WebRequest -Uri "$BaseUrl$Path" -WebSession $Session -UseBasicParsing -MaximumRedirection 5
        return @{ Status = [int]$r.StatusCode; Body = $r.Content; Url = $r.BaseResponse.ResponseUri.ToString() }
    } catch [System.Net.WebException] {
        $resp = $_.Exception.Response
        if ($resp) {
            $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $body = $sr.ReadToEnd(); $sr.Close()
            return @{ Status = [int]$resp.StatusCode; Body = $body; Url = $resp.ResponseUri.ToString() }
        }
        return @{ Status = -1; Body = $_.Exception.Message; Url = "" }
    }
}

function Encode-Form {
    param([hashtable]$Fields, [string]$Token)
    $parts = New-Object System.Collections.ArrayList
    if ($Token) { [void]$parts.Add("__RequestVerificationToken=" + [System.Web.HttpUtility]::UrlEncode($Token)) }
    foreach ($k in $Fields.Keys) {
        $v = $Fields[$k]
        $ek = [System.Web.HttpUtility]::UrlEncode($k)
        if ($v -is [System.Array] -or $v -is [System.Collections.IList]) {
            foreach ($item in $v) {
                [void]$parts.Add($ek + "=" + [System.Web.HttpUtility]::UrlEncode([string]$item))
            }
        } else {
            [void]$parts.Add($ek + "=" + [System.Web.HttpUtility]::UrlEncode([string]$v))
        }
    }
    return [string]::Join("&", $parts.ToArray())
}

function Post-Form {
    param($Session, [string]$Path, [hashtable]$Fields, [string]$Token)
    $body = Encode-Form -Fields $Fields -Token $Token
    try {
        $r = Invoke-WebRequest -Uri "$BaseUrl$Path" -Method POST -Body $body `
            -ContentType "application/x-www-form-urlencoded" `
            -WebSession $Session -UseBasicParsing -MaximumRedirection 5
        return @{ Status = [int]$r.StatusCode; Body = $r.Content; Url = $r.BaseResponse.ResponseUri.ToString() }
    } catch [System.Net.WebException] {
        $resp = $_.Exception.Response
        if ($resp) {
            $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $eb = $sr.ReadToEnd(); $sr.Close()
            return @{ Status = [int]$resp.StatusCode; Body = $eb; Url = $resp.ResponseUri.ToString() }
        }
        return @{ Status = -1; Body = $_.Exception.Message; Url = "" }
    }
}

function Post-Multipart {
    param($Session, [string]$Path, [hashtable]$Fields, [string]$Token, [string]$ImageField, [string]$ImageFilePath)
    $boundary = "----PSBoundary" + [Guid]::NewGuid().ToString("N")
    $LF = "`r`n"
    $enc = [System.Text.Encoding]::UTF8
    $ms = New-Object System.IO.MemoryStream

    function Write-Part([System.IO.MemoryStream]$ms, [string]$s) {
        $b = [System.Text.Encoding]::UTF8.GetBytes($s)
        $ms.Write($b, 0, $b.Length)
    }

    if ($Token) {
        Write-Part $ms "--$boundary$LF"
        Write-Part $ms "Content-Disposition: form-data; name=`"__RequestVerificationToken`"$LF$LF"
        Write-Part $ms "$Token$LF"
    }
    foreach ($k in $Fields.Keys) {
        $v = $Fields[$k]
        if ($v -is [System.Array] -or $v -is [System.Collections.IList]) {
            foreach ($item in $v) {
                Write-Part $ms "--$boundary$LF"
                Write-Part $ms "Content-Disposition: form-data; name=`"$k`"$LF$LF"
                Write-Part $ms "$([string]$item)$LF"
            }
        } else {
            Write-Part $ms "--$boundary$LF"
            Write-Part $ms "Content-Disposition: form-data; name=`"$k`"$LF$LF"
            Write-Part $ms "$([string]$v)$LF"
        }
    }
    if ($ImageFilePath -and (Test-Path $ImageFilePath)) {
        Write-Part $ms "--$boundary$LF"
        Write-Part $ms "Content-Disposition: form-data; name=`"$ImageField`"; filename=`"upload.jpg`"$LF"
        Write-Part $ms "Content-Type: image/jpeg$LF$LF"
        $bytes = [IO.File]::ReadAllBytes($ImageFilePath)
        $ms.Write($bytes, 0, $bytes.Length)
        Write-Part $ms "$LF"
    }
    Write-Part $ms "--$boundary--$LF"

    $bodyBytes = $ms.ToArray()
    $ms.Close()

    try {
        $r = Invoke-WebRequest -Uri "$BaseUrl$Path" -Method POST -Body $bodyBytes `
            -ContentType "multipart/form-data; boundary=$boundary" `
            -WebSession $Session -UseBasicParsing -MaximumRedirection 5
        return @{ Status = [int]$r.StatusCode; Body = $r.Content; Url = $r.BaseResponse.ResponseUri.ToString() }
    } catch [System.Net.WebException] {
        $resp = $_.Exception.Response
        if ($resp) {
            $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $eb = $sr.ReadToEnd(); $sr.Close()
            return @{ Status = [int]$resp.StatusCode; Body = $eb; Url = $resp.ResponseUri.ToString() }
        }
        return @{ Status = -1; Body = $_.Exception.Message; Url = "" }
    }
}

function Log-Result {
    param([string]$Step, [string]$Result, [string]$Detail = "")
    $script:Results += [PSCustomObject]@{ Step = $Step; Result = $Result; Detail = $Detail }
    $color = if ($Result -eq "PASS") { "Green" } elseif ($Result -eq "FAIL") { "Red" } else { "Yellow" }
    Write-Host ("[{0}] {1} {2}" -f $Result, $Step, $(if ($Detail) { "- $Detail" } else { "" })) -ForegroundColor $color
}

# ================================
# WAIT FOR APP
# ================================
Write-Host "`n=== Cho app khoi dong ===" -ForegroundColor Cyan
$ready = $false
for ($i = 0; $i -lt 60; $i++) {
    try {
        $r = Invoke-WebRequest -Uri "$BaseUrl/" -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
        if ($r.StatusCode -eq 200) { $ready = $true; break }
    } catch {}
    Start-Sleep -Seconds 1
}
if (-not $ready) { Write-Host "APP NOT READY" -ForegroundColor Red; exit 1 }
Write-Host "App ready" -ForegroundColor Green

# ================================
# 1. LOGIN AS HOST + ĐĂNG TIN 5 CĂN
# ================================
Write-Host "`n=== LUONG 1: Host dang nhap + dang 5 can ho ===" -ForegroundColor Cyan
$hostSession = $null
$hostPage = Invoke-WebRequest -Uri "$BaseUrl/Home/Login" -UseBasicParsing -SessionVariable hostSession
$token = Get-AntiforgeryToken -Html $hostPage.Content
if (-not $token) { Log-Result "GET /Home/Login" "FAIL" "Khong lay duoc antiforgery token"; exit 1 }
Log-Result "GET /Home/Login" "PASS"

$login = Post-Form -Session $hostSession -Path "/Home/Login" -Token $token -Fields @{
    Email = "host@luxehaven.vn"; Password = "Host@123"; RememberMe = "false"
}

$loginCookieExists = $false
foreach ($c in $hostSession.Cookies.GetCookies([Uri]"$BaseUrl/")) {
    if ($c.Name -like ".AspNetCore.Identity.Application*") { $loginCookieExists = $true; break }
}
if ($loginCookieExists) {
    Log-Result "POST /Home/Login as host" "PASS" ("Status=" + $login.Status + ", auth cookie set")
} else {
    Log-Result "POST /Home/Login as host" "FAIL" ("Status=" + $login.Status + ", no auth cookie")
}

# Đăng 5 căn
$listings = @(
    @{ Title = "TEST E2E - Studio Ba Đình view Lăng Bác"; Address = "Đường Hùng Vương, Phường Quán Thánh, Ba Đình, Hà Nội"; RegionId = 2; CategoryId = 1; Price = 9500000; Area = 32; Bedrooms = 1; Bathrooms = 1; AmenityIds = @(1,2,5,6) }
    @{ Title = "TEST E2E - Nhà nguyên căn Bình Thạnh có sân vườn"; Address = "Đường D5, Phường 25, Bình Thạnh, TP. Hồ Chí Minh"; RegionId = 3; CategoryId = 2; Price = 18000000; Area = 110; Bedrooms = 3; Bathrooms = 2; AmenityIds = @(1,2,3,5,9) }
    @{ Title = "TEST E2E - Penthouse Mỹ Khê view biển trực diện"; Address = "Võ Nguyên Giáp, Phường Mỹ Khê, Sơn Trà, Đà Nẵng"; RegionId = 1; CategoryId = 4; Price = 35000000; Area = 180; Bedrooms = 4; Bathrooms = 3; AmenityIds = @(1,2,3,4,5,6,7,8,9) }
    @{ Title = "TEST E2E - Phòng trọ Thanh Xuân gần ĐH Quốc Gia"; Address = "Nguyễn Trãi, Phường Thanh Xuân Bắc, Thanh Xuân, Hà Nội"; RegionId = 2; CategoryId = 5; Price = 3500000; Area = 18; Bedrooms = 1; Bathrooms = 1; AmenityIds = @(1,2,6) }
    @{ Title = "TEST E2E - Biệt thự Riverside Quận 7 hồ bơi riêng"; Address = "Nguyễn Lương Bằng, Phường Tân Phú, Quận 7, TP. Hồ Chí Minh"; RegionId = 3; CategoryId = 3; Price = 65000000; Area = 320; Bedrooms = 5; Bathrooms = 4; AmenityIds = @(1,2,3,4,5,6,7,8,9) }
)

foreach ($l in $listings) {
    $formPage = Get-Page -Session $hostSession -Path "/Home/PostListing"
    if ($formPage.Status -ne 200) {
        Log-Result ("POST listing: " + $l.Title.Substring(7, [Math]::Min(50,$l.Title.Length-7))) "FAIL" ("GET form status=" + $formPage.Status)
        continue
    }
    $tok = Get-AntiforgeryToken -Html $formPage.Body

    $fields = @{
        Title = $l.Title
        Description = "Mô tả test E2E cho căn hộ. Vị trí thuận tiện, đầy đủ nội thất, giá hợp lý. Có ban công thoáng mát view đẹp. Phù hợp cho gia đình hoặc cá nhân muốn ở dài hạn."
        CategoryId = $l.CategoryId
        Area = $l.Area
        Bedrooms = $l.Bedrooms
        Bathrooms = $l.Bathrooms
        Price = $l.Price
        DefaultDeposit = $l.Price * 2
        FeeNote = "Phí dịch vụ 100k/tháng"
        Address = $l.Address
        RegionId = $l.RegionId
        AmenityIds = $l.AmenityIds
        CoverImageIndex = 0
    }
    $resp = Post-Multipart -Session $hostSession -Path "/Home/PostListing" `
        -Token $tok -Fields $fields -ImageField "Images" -ImageFilePath $ImagePath

    $shortTitle = $l.Title.Substring(7, [Math]::Min(45,$l.Title.Length-7))
    if ($resp.Url -notmatch "PostListing$") {
        Log-Result ("POST listing: $shortTitle") "PASS" ("redirect to " + $resp.Url.Substring($resp.Url.LastIndexOf('/')))
    } elseif ($resp.Body -match "field-validation-error|validation-summary-errors") {
        $errs = [regex]::Matches($resp.Body, '<span[^>]*class="[^"]*(?:field-validation-error|validation-summary-errors)[^"]*"[^>]*>([^<]+)</span>') |
                ForEach-Object { $_.Groups[1].Value.Trim() } |
                Where-Object { $_ -ne "" } | Select-Object -Unique | Select-Object -First 5
        Log-Result ("POST listing: $shortTitle") "FAIL" ("Errors: " + ($errs -join ' | '))
    } else {
        Log-Result ("POST listing: $shortTitle") "FAIL" ("Status=" + $resp.Status + ", URL=" + $resp.Url)
    }
}

# ================================
# 2. RENTER LOGIN + BOOK VIEWING + FAVORITE
# ================================
Write-Host "`n=== LUONG 2: Renter dat lich xem + favorite ===" -ForegroundColor Cyan
try { Invoke-WebRequest -Uri "$BaseUrl/Home/Logout" -UseBasicParsing -WebSession $hostSession -ErrorAction Stop | Out-Null } catch {}

$renterSession = $null
$rpage = Invoke-WebRequest -Uri "$BaseUrl/Home/Login" -UseBasicParsing -SessionVariable renterSession
$token = Get-AntiforgeryToken -Html $rpage.Content
$login = Post-Form -Session $renterSession -Path "/Home/Login" -Token $token -Fields @{
    Email = "renter@luxehaven.vn"; Password = "Renter@123"; RememberMe = "false"
}
$cookies = $renterSession.Cookies.GetCookies([Uri]"$BaseUrl/")
$hasAuth = $false; foreach ($c in $cookies) { if ($c.Name -like ".AspNetCore.Identity.Application*") { $hasAuth = $true } }
if ($hasAuth) { Log-Result "POST Login renter" "PASS" } else { Log-Result "POST Login renter" "FAIL" ("Status=" + $login.Status) }

# Lấy ID căn vừa tạo
$conn = New-Object System.Data.SqlClient.SqlConnection("Server=LAPTOP-S5A6Q2L5\SQLEXPRESS;Database=LuxeHavenDb;User Id=sa;Password=sa;TrustServerCertificate=True;")
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT TOP 5 Id, TieuDe FROM CanHo WHERE TieuDe LIKE 'TEST E2E%' ORDER BY Id"
$rd = $cmd.ExecuteReader()
$newIds = @()
while ($rd.Read()) { $newIds += @{ Id = $rd.GetInt32(0); Title = $rd.GetString(1) } }
$rd.Close(); $conn.Close()
Log-Result "QUERY new CanHo IDs" $(if ($newIds.Count -ge 1) { "PASS" } else { "FAIL" }) ("Tim thay " + $newIds.Count + " can")

# Đặt lịch xem 1 căn đầu
if ($newIds.Count -ge 1) {
    $target = $newIds[0]
    $detailPage = Get-Page -Session $renterSession -Path ("/Home/ApartmentDetail/" + $target.Id)
    $tok = Get-AntiforgeryToken -Html $detailPage.Body
    $tomorrow = (Get-Date).AddDays(3).ToString("yyyy-MM-dd")
    $book = Post-Form -Session $renterSession -Path "/Home/BookViewing" -Token $tok -Fields @{
        ApartmentId = $target.Id
        ContactName = "Nguyễn Văn Test"
        ContactPhone = "0987654321"
        ContactEmail = "test.e2e@luxehaven.vn"
        ScheduledDate = $tomorrow
        SlotHour = 14
        Note = "Đặt lịch xem qua test E2E - vui lòng liên hệ trước khi xem"
    }
    if ($book.Status -ge 200 -and $book.Status -lt 400) {
        Log-Result ("POST BookViewing CanHo " + $target.Id) "PASS"
    } else {
        Log-Result ("POST BookViewing CanHo " + $target.Id) "FAIL" ("Status=" + $book.Status)
    }
}

# Toggle favorite 2 căn
if ($newIds.Count -ge 3) {
    foreach ($idx in 1, 2) {
        $target = $newIds[$idx]
        $detailPage = Get-Page -Session $renterSession -Path ("/Home/ApartmentDetail/" + $target.Id)
        $tok = Get-AntiforgeryToken -Html $detailPage.Body
        $fav = Post-Form -Session $renterSession -Path "/Favorites/Toggle" -Token $tok -Fields @{
            apartmentId = $target.Id
            returnUrl = "/"
        }
        if ($fav.Status -ge 200 -and $fav.Status -lt 400) {
            Log-Result ("POST Favorites/Toggle CanHo " + $target.Id) "PASS"
        } else {
            Log-Result ("POST Favorites/Toggle CanHo " + $target.Id) "FAIL" ("Status=" + $fav.Status)
        }
    }
}

# ================================
# 3. ĐĂNG KÝ USER MỚI
# ================================
Write-Host "`n=== LUONG 3: Dang ky user moi ===" -ForegroundColor Cyan
try { Invoke-WebRequest -Uri "$BaseUrl/Home/Logout" -UseBasicParsing -WebSession $renterSession -ErrorAction Stop | Out-Null } catch {}

$newSession = $null
$regPage = Invoke-WebRequest -Uri "$BaseUrl/Home/Register" -UseBasicParsing -SessionVariable newSession
$tok = Get-AntiforgeryToken -Html $regPage.Content
$randomEmail = "e2e_user_$(Get-Random -Maximum 99999)@luxehaven.vn"
$reg = Post-Form -Session $newSession -Path "/Home/Register" -Token $tok -Fields @{
    FullName = "Người dùng E2E Test"
    Email = $randomEmail
    Phone = "0912345678"
    Password = "Test@1234"
    ConfirmPassword = "Test@1234"
}
$cookies = $newSession.Cookies.GetCookies([Uri]"$BaseUrl/")
$hasAuth = $false; foreach ($c in $cookies) { if ($c.Name -like ".AspNetCore.Identity.Application*") { $hasAuth = $true } }
if ($reg.Body -match "field-validation-error|validation-summary-errors") {
    $errs = [regex]::Matches($reg.Body, '<span[^>]*class="[^"]*(?:field-validation-error|validation-summary-errors)[^"]*"[^>]*>([^<]+)</span>') |
            ForEach-Object { $_.Groups[1].Value.Trim() } |
            Where-Object { $_ -ne "" } | Select-Object -Unique
    Log-Result ("POST Register " + $randomEmail) "FAIL" ("Errors: " + ($errs -join ' | '))
} elseif ($hasAuth) {
    Log-Result ("POST Register " + $randomEmail) "PASS" "auth cookie set sau khi tao tai khoan"
} else {
    Log-Result ("POST Register " + $randomEmail) "PASS" ("URL=" + $reg.Url)
}

# ================================
# SUMMARY
# ================================
Write-Host "`n=== TONG KET ===" -ForegroundColor Cyan
$pass = ($script:Results | Where-Object Result -eq "PASS").Count
$fail = ($script:Results | Where-Object Result -eq "FAIL").Count
$total = $script:Results.Count
Write-Host ("Total=$total | PASS=$pass | FAIL=$fail") -ForegroundColor $(if ($fail -eq 0) { "Green" } else { "Red" })
if ($fail -gt 0) { exit 1 } else { exit 0 }
