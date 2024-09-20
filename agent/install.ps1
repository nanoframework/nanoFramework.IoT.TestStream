param (
    [switch]$SkipWSLInstallation,
    [switch]$SkipDockerInstallation,
    [switch]$SkipUSBIPDInstallation,
    [string]$WSLDistribution = "Ubuntu"
)

# Define a function to convert Windows path to Linux path
function Convert-WindowsPathToLinuxPath {
    param (
        [string]$windowsPath
    )
    $windowsPath = $windowsPath -replace '\\', '/'
    $windowsPath = $windowsPath -replace '^([A-Za-z]):', '/mnt/$1'
    if ($windowsPath.Length -ge 6) {
        $windowsPath = $windowsPath.Substring(0, 5) + ([string]$windowsPath[5]).ToLower() + $windowsPath.Substring(6)
    }
    return $windowsPath
}

if (-not $SkipWSLInstallation) {
    # Check if WSL is installed
    try {
        wsl --list --verbose
        Write-Output "WSL is already installed."
    } catch {
        Write-Output "WSL is not installed. Installing WSL..."
        wsl --install
    }

    # Install Ubuntu in WSL
    Write-Output "Installing $WSLDistribution in WSL."
    Write-Output "Please follow the instructions, create the user, and set the password."
    Write-Output "After the installation is complete, please write 'exit' so that the script can continue."
    wsl --install -d $WSLDistribution
    Write-Output "$WSLDistribution installation complete."
} else {
    Write-Output "Skipping WSL installation as per the parameter."
}

# Define the Windows path
$windowsPath = Get-Location
# Replace backslashes with forward slashes and replace C: with /mnt/c
$linuxPath = Convert-WindowsPathToLinuxPath -windowsPath $windowsPath

if (-not $SkipWSLInstallation) {
    
    # Install Docker in WSL
    Write-Output "Installing Docker in WSL..."
    $dockerScript = $linuxPath + "/install/docker.sh"
    wsl -d $WSLDistribution -- chmod +x $dockerScript
    Write-Output "Once you'll be in WSL, please run the following command to install Docker:"
    Write-Output "sudo ./install/docker.sh"
    Write-Output "You will be prompted for your password during the installation."
    Write-Output "After the installation is complete, please write 'exit' so that the script can continue."
    wsl -d $WSLDistribution
    Write-Output "Docker instalation completed."
} else {
    Write-Output "Skipping Docker installation as per the parameter."
}

Write-Output "Setting up devices rulesets..."
# Set the permissions for the scripts
$deviceRules = $linuxPath + "/install/devices.sh"
wsl -d $WSLDistribution -u root -- chmod +x $deviceRules
wsl -d $WSLDistribution -u root -- $deviceRules
wsl -d $WSLDistribution -u root -- service udev restart

if (-not $SkipUSBIPDInstallation) {
    # Install usbipd
    Write-Output "Installing usbipd..."
    $msiUrl = "https://github.com/dorssel/usbipd-win/releases/download/v4.3.0/usbipd-win_4.3.0.msi"
    $msiPath = "$env:TEMP\usbipd-win_4.3.0.msi"
    # Download the MSI
    Invoke-WebRequest -Uri $msiUrl -OutFile $msiPath
    # Install the MSI
    Start-Process -FilePath "msiexec.exe" -ArgumentList "/i", $msiPath, "/quiet", "/norestart" -Wait
    Write-Output "usbipd installation complete."
} else {
    Write-Output "Skipping usbipd installation as per the parameter."
}

Write-Output "Agent requirements installation complete."