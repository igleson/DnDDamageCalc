#!/bin/bash

# Development script with hot reloading
# This runs the app with dotnet watch for .NET hot reload + custom template/CSS hot reload

echo "🔥 Starting D&D Damage Calculator with Hot Reload..."
echo ""
echo "Features enabled:"
echo "  • .NET code hot reload (dotnet watch)"
echo "  • Scriban template hot reload"
echo "  • CSS hot reload"
echo "  • Browser auto-refresh on changes"
echo ""
echo "The app will be available at: http://localhost:5082"
echo "Press Ctrl+C to stop"
echo ""

# Set environment to Development
export ASPNETCORE_ENVIRONMENT=Development

# Run with dotnet watch for .NET hot reload
cd src/DnDDamageCalc.Web
dotnet watch run