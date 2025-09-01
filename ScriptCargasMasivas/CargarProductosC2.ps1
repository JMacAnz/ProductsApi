# CargarProductosC2.ps1
param (
    [int]$TotalProductos = 10
)

# ===============================
# CONFIGURACIÓN
# ===============================
$AuthUrl     = "http://localhost:5000/api/Auth/login"
$ProductUrl  = "http://localhost:5000/api/Product"
$Email       = "admin@asisya.com"
$Password    = "password123"

# ===============================
# FUNCIÓN: Obtener token
# ===============================
function Get-AuthToken {
    Write-Host "Solicitando token de autenticación..."

    $body = @{
        email    = $Email
        password = $Password
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri $AuthUrl -Method Post -Body $body -ContentType "application/json"
        if (-not $response -or -not $response.success) {
            throw "No se recibió respuesta válida del servidor."
        }
        return $response.data.token
    }
    catch {
        Write-Error "Error al obtener token: $($_.Exception.Message)"
        exit 1
    }
}

# ===============================
# OBTENER TOKEN
# ===============================
$jwtToken = Get-AuthToken
Write-Host "Token obtenido correctamente."

# ===============================
# CREAR PRODUCTOS
# ===============================
for ($i = 1; $i -le $TotalProductos; $i++) {
    $producto = @{
        name        = "Producto-$i"
        description = "Descripción del producto $i"
        price       = 1000 + ($i * 10)
        stock       = 50 + $i
        sku         = "SKU-cat-2-$i"
        categoryId  = 2
        isActive    = $true
    } | ConvertTo-Json -Depth 10

    try {
        $response = Invoke-RestMethod -Uri $ProductUrl -Method Post `
            -Headers @{ "Authorization" = "Bearer $jwtToken"; "Content-Type" = "application/json" } `
            -Body $producto

        if ($response.success) {
            Write-Host "Producto $i creado con éxito. ID devuelto: $($response.data.id)"
        }
        else {
            Write-Warning "Producto $i no se creó: $($response.message)"
        }
    }
    catch {
        if ($_.Exception.Response -ne $null) {
            $resp = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($resp)
            $body = $reader.ReadToEnd() | ConvertFrom-Json
            Write-Warning ("Producto $i no se creó: {0}" -f $body.message)
        }
        else {
            Write-Error ("Error creando producto {0}: {1}" -f $i, $_.Exception.Message)
        }
    }
}
