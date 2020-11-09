#!/bin/bash

echo "Building all projects"
dotnet clean
dotnet build

echo "Running tests"
dotnet test

echo "Running Gridwich.Host.FunctionApp project"
cd Gridwich.Host.FunctionApp/src/bin/Debug/netcoreapp3.1
func start
