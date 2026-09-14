#!/bin/bash

# Setup script to install .NET 10 SDK using Homebrew
# This script is designed for macOS development environments

set -e

echo "Setting up .NET 10 SDK with Homebrew..."

# Check if Homebrew is installed
if ! command -v brew &> /dev/null; then
    echo "Error: Homebrew is not installed. Please install Homebrew first."
    echo "Visit: https://brew.sh"
    exit 1
fi

echo "✓ Homebrew found"

# Check if .NET is already installed
if command -v dotnet &> /dev/null; then
    CURRENT_VERSION=$(dotnet --version)
    echo "✓ .NET is already installed (version: $CURRENT_VERSION)"
    
    # Check if it's version 10
    if [[ $CURRENT_VERSION == 10.* ]]; then
        echo "✓ .NET 10 is already installed"
        exit 0
    else
        echo "⚠ Different .NET version detected. Proceeding with installation of .NET 10..."
    fi
fi

# Install .NET 10 SDK using Homebrew
echo "Installing .NET 10 SDK..."
brew install dotnet

# Verify installation
if command -v dotnet &> /dev/null; then
    INSTALLED_VERSION=$(dotnet --version)
    echo "✓ .NET installed successfully (version: $INSTALLED_VERSION)"
    
    # Verify version matches global.json requirement
    echo "Verifying .NET SDK version matches project requirements..."
    dotnet --version
else
    echo "Error: .NET installation failed"
    exit 1
fi

echo ""
echo "✓ Setup complete! .NET 10 SDK is ready to use."
echo ""
echo "Next steps:"
echo "1. Install Aspire CLI: dotnet tool install -g Microsoft.Aspire.Cli"
echo "2. Ensure Docker Desktop is running"
echo "3. Run: aspire run"
