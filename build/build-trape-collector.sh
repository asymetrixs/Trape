#!/bin/bash

BASEDIR=$(readlink -f $(dirname $0))
SOURCEDIR=$BASEDIR/../src
CURRENTPROJECT=

function error()
{
	echo
	echo "FAILED in ${CURRENTPROJECT}"
	exit 1
}

trap error ERR

echo "Building Trape Collector"
CURRENTPROJEFT="Trape Collector"
PROFILE=$1

if [ -z $PROFILE ]; then
	PROFILE=Debug
elif [[ "d" == "$PROFILE" ]]; then
	PROFILE=Debug
elif [[ "r" == "$PROFILE" ]]; then
	PROFILE=Release
else
	echo "Provided argument '$1' was not understood"
	exit 1
fi

echo "Building Profile: $PROFILE"

cd $SOURCEDIR/trape/Trape.Cli.Collector

# Building
dotnet-sdk.dotnet clean
dotnet-sdk.dotnet build --configuration:${PROFILE} --runtime:ubuntu.18.04-x64

cd $BASEDIR

echo "Packing Trape Collector"
cd $BASEDIR

# Creating directory structure

VERSION=0.1-1
PACKINGDIR=$BASEDIR/packaging/trape-collector_$VERSION
rm -rf $PACKINGDIR
rm -rf trape-collector_$VERSION.deb
mkdir -p $PACKINGDIR/opt/trape/trape-collector/
mkdir -p $PACKINGDIR/DEBIAN
cp -r $SOURCEDIR/trape/Trape.Cli.Collector/bin/Debug/netcoreapp3.1/ubuntu.18.04-x64/* $PACKINGDIR/opt/trape/trape-collector/
chmod 664 $PACKINGDIR/opt/trape/trape-collector/*
chmod 744 $PACKINGDIR/opt/trape/trape-collector/Trape.Cli.Collector


cat << EOF > $PACKINGDIR/DEBIAN/control
Package: trape-collector
Version: $VERSION
Section: base
Priority: optional
Architecture: x86-64
Depends: 
Maintainer: Damian Wolgast <damian.wolgast@asymetrixs.net>
Description: Trape Collector is the data collector for the Trape Trader.
 It connects to binance and write the realtime data into the database.
EOF

cd $BASEDIR/packaging
dpkg-deb -v --build trape-collector_$VERSION


