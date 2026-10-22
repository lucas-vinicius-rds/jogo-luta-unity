param([Parameter(Mandatory=$true)][string]$CodeFile,[string]$OutputFile)
$code=[string](Get-Content -LiteralPath $CodeFile -Raw)
$result=Invoke-RestMethod 'http://127.0.0.1:7890/api/editor/execute-code' -Method Post -ContentType 'application/json' -Body (@{code=$code}|ConvertTo-Json) -TimeoutSec 60
$json=$result | ConvertTo-Json -Depth 25
if($OutputFile){$json | Set-Content -Encoding utf8 $OutputFile}
$json

