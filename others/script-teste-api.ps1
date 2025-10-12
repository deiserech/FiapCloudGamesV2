
# Script PowerShell para cadastrar usuário e fazer requisições em loop com autenticação JWT

$baseUrl = "https://fiapcloudgames-api-enhjgnftfte0gxcd.brazilsouth-01.azurewebsites.net/"
$iterations = 100


# 1. Cadastrar um usuário via POST api/User
# Gera um email aleatório para evitar duplicidade
$random = -join ((65..90) + (97..122) | Get-Random -Count 8 | % {[char]$_})
$randomEmail = "testeuser_$random@example.com"
$userBody = @{
    name = "Teste User"
    email = $randomEmail
    password = "SenhaForte123!"
    role = "Admin"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "$baseUrl/api/User" -Method Post -Body $userBody -ContentType "application/json"
Write-Host "Usuário cadastrado:"
$response | ConvertTo-Json


# 2. Login para obter o token JWT
$loginBody = @{
    email = $randomEmail
    password = "SenhaForte123!"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token  # Ajuste o nome do campo se necessário

# Buscar userId do usuário autenticado
$profile = Invoke-RestMethod -Uri "$baseUrl/api/User/profile" -Method Get -Headers @{ Authorization = "Bearer $token" }
$userId = $profile.Id

# 3. Loop de requisições autenticadas
for ($i = 1; $i -le $iterations; $i++) {
    Write-Host "Iteração $i"

    # GET api/Game
    Invoke-RestMethod -Uri "$baseUrl/api/Game" -Method Get -Headers @{ Authorization = "Bearer $token" }

    # POST api/Game
    $gameBody = @{
        title = "Novo Jogo $i"
        genre = "Aventura"
        price = 99.99
    } | ConvertTo-Json
    $gameResponse = $null
    try {
        $gameResponse = Invoke-RestMethod -Uri "$baseUrl/api/Game" -Method Post -Body $gameBody -ContentType "application/json" -Headers @{ Authorization = "Bearer $token" }
    } catch {
        Write-Host "Erro ao criar jogo: $($_.Exception.Message)"
        if ($_.Exception.Response -ne $null) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $reader.ReadToEnd() | Write-Host
        }
    }
    $gameId = if ($gameResponse) { $gameResponse.Id } else { $i }

    # GET api/Game/{gameId} - Buscar o jogo recém-criado
    try {
        $gameGet = Invoke-RestMethod -Uri "$baseUrl/api/Game/$gameId" -Method Get -Headers @{ Authorization = "Bearer $token" }
        Write-Host "Jogo criado (GET):" ($gameGet | ConvertTo-Json)
    } catch {
    Write-Host "Erro ao buscar jogo ${gameId}: $($_.Exception.Message)"
        if ($_.Exception.Response -ne $null) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $reader.ReadToEnd() | Write-Host
        }
    }


    # GET api/User/profile
    Invoke-RestMethod -Uri "$baseUrl/api/User/profile" -Method Get -Headers @{ Authorization = "Bearer $token" }

    # GET api/Library/user/{userId}
    Invoke-RestMethod -Uri "$baseUrl/api/Library/user/$userId" -Method Get -Headers @{ Authorization = "Bearer $token" }

    # Criar 100 promoções ativas para o game criado
    for ($p = 1; $p -le 3; $p++) {
        $promotionBody = @{
            gameId = $gameId
            title = "Promoção $p do Jogo $gameId"
            description = "Desconto especial $p para o jogo $gameId"
            discountPercentage = 10 + ($p % 50) # Varia de 10 a 59
            startDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
            endDate = (Get-Date).AddDays(30).ToString("yyyy-MM-ddTHH:mm:ssZ")
            isActive = $true
        } | ConvertTo-Json
        $promoUrl = ("{0}/api/Promotion" -f $baseUrl.TrimEnd('/'))
        Write-Host "Criando promoção $p para o jogo ${gameId} na URL: $promoUrl"
        try {
            Invoke-RestMethod -Uri $promoUrl -Method Post -Body $promotionBody -ContentType "application/json" -Headers @{ Authorization = "Bearer $token" }
        } catch {
            Write-Host "Erro ao criar promoção $p para o jogo ${gameId}: $($_.Exception.Message)"
            if ($_.Exception.Response -ne $null) {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $reader.ReadToEnd() | Write-Host
            }
        }
    }

    # Buscar promoções ativas para o game criado
    try {
        $activePromos = Invoke-RestMethod -Uri "$baseUrl/api/Promotion/game/$gameId/active" -Method Get -Headers @{ Authorization = "Bearer $token" }
        Write-Host "Promoções ativas para o jogo ${gameId}:" ($activePromos | ConvertTo-Json)
    } catch {
        Write-Host "Erro ao buscar promoções ativas do jogo ${gameId}: $($_.Exception.Message)"
        if ($_.Exception.Response -ne $null) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $reader.ReadToEnd() | Write-Host
        }
    }
}
