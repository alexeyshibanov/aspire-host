$certPath = "$PSScriptRoot\.certs\ca.crt"

if (Test-Path $certPath) {
    Write-Host "Installing Aspire Elastic CA to Trusted Root..."
    Import-Certificate -FilePath $certPath -CertStoreLocation Cert:\LocalMachine\Root
    Write-Host "Done! Restart your browser."
} else {
    Write-Error "Certificate not found. Run the Aspire host at least once first."
}
