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

function Install-Dos2Unix {
    param (
        [string]$WSLDistribution
    )

    # Check if dos2unix is installed
    $dos2unixCheck = wsl -d $WSLDistribution -- bash -c "if command -v dos2unix >/dev/null 2>&1; then echo 'installed'; else echo 'not installed'; fi"

    if ($dos2unixCheck -eq "not installed") {
        Write-Output "dos2unix is not installed. Installing dos2unix...You will be prompted for your password twice during the installation."
        wsl -d $WSLDistribution -- sudo apt update
        wsl -d $WSLDistribution -- sudo apt install -y dos2unix
    } else {
        Write-Output "dos2unix is already installed. Skipping installation."
    }
}

if (-not $SkipWSLInstallation) {
    Write-Output "------------------------------------------"
    Write-Output "---- Step 1: Checking WSL installation..."
    Write-Output "------------------------------------------"
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
    Write-Host "After the installation is complete, please write 'exit' so that the script can continue." -ForegroundColor DarkYellow
    Write-Output "------------------------------------------"
    wsl --install -d $WSLDistribution
    Write-Output "$WSLDistribution installation complete."
    Write-Output "------------------------------------------"
    Write-Output ""
} else {
    Write-Output "Skipping WSL installation as per the parameter."
    Write-Output "------------------------------------------"
    Write-Output ""
}

# Define the Windows path
$windowsPath = Get-Location
# Replace backslashes with forward slashes and replace C: with /mnt/c
$linuxPath = Convert-WindowsPathToLinuxPath -windowsPath $windowsPath

# Check if the install directory exists
$installPath = Join-Path -Path $windowsPath -ChildPath "install"
$agentInstallPath = Join-Path -Path $windowsPath -ChildPath "agent\install"
$needAgent = $false

if (Test-Path -Path $installPath) {
    # all good
} elseif (Test-Path -Path $agentInstallPath) {
    $linuxPath = $linuxPath + "/agent"
    $needAgent = $true
} else {
    Write-Host "Neither $installPath nor $agentInstallPath exists." -ForegroundColor Red
}

if (-not $SkipDockerInstallation) {    
    Write-Output "------------------------------------------"
    Write-Output "---- Step 2: Checking Docker installation..."
    Write-Output "------------------------------------------"
    # Install Docker in WSL
    Write-Output "Installing Docker in WSL..."
    $dockerScript = $linuxPath + "/install/docker.sh"
    wsl -d $WSLDistribution -- chmod +x $dockerScript

    Write-Output "Converting file line endings to LF format in $linuxPath..."
    Install-Dos2Unix -WSLDistribution $WSLDistribution
    wsl -d $WSLDistribution -- bash -c "find '$linuxPath' -type f -exec dos2unix {} \\;"

    Write-Output "Once you'll be in WSL, please run the following command to install Docker:"
if($needAgent) {
    Write-Host "  sudo ./agent/install/docker.sh" -ForegroundColor DarkYellow
} else {
    Write-Host "  sudo ./install/docker.sh" -ForegroundColor DarkYellow
}
    Write-Host "You will be prompted for your password during the installation." -ForegroundColor DarkYellow
    Write-Output "After the installation is complete, please write 'exit' so that the script can continue."
    wsl -d $WSLDistribution
    Write-Output "Docker instalation completed."
    Write-Output "------------------------------------------"
    Write-Output ""
} else {
    Write-Output "Skipping Docker installation as per the parameter."
    Write-Output "------------------------------------------"
    Write-Output ""
}

Write-Output "Setting up devices rulesets..."
# Set the permissions for the scripts
$deviceRules = $linuxPath + "/install/devices.sh"
wsl -d $WSLDistribution -u root -- chmod +x $deviceRules
wsl -d $WSLDistribution -u root -- $deviceRules
wsl -d $WSLDistribution -u root -- service udev restart

if (-not $SkipUSBIPDInstallation) {
    Write-Output "------------------------------------------"
    Write-Output "---- Step 3: Installing USBIPD..."
    Write-Output "------------------------------------------"
    # Install usbipd
    Write-Output "Installing usbipd..."
    $msiUrl = "https://github.com/dorssel/usbipd-win/releases/download/v4.3.0/usbipd-win_4.3.0.msi"
    $msiPath = "$env:TEMP\usbipd-win_4.3.0.msi"
    # Download the MSI
    Invoke-WebRequest -Uri $msiUrl -OutFile $msiPath
    # Install the MSI
    Start-Process -FilePath "msiexec.exe" -ArgumentList "/i", $msiPath, "/quiet", "/norestart" -Wait
    Write-Output "usbipd installation complete."
    Write-Output "------------------------------------------"
    Write-Output ""
} else {
    Write-Output "Skipping usbipd installation as per the parameter."
    Write-Output "------------------------------------------"
    Write-Output ""
}

Write-Output "Agent requirements installation complete."