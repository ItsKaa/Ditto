#!/bin/sh

if hash dotnet 2>/dev/null
then
	#echo "Dotnet installed."
	echo ""
else
	echo "Dotnet is not installed. Please install dotnet."
	exit 1
fi

cd "..\Kaa.Discord"
dotnet run --configuration Release

echo "Done"
read
exit 0