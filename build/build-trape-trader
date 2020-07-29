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

echo "Building Trape Trader"
CURRENTPROJECT="Trape Trader"
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

cd $SOURCEDIR/trape/Trape.Cli.Trader

# Building
dotnet-sdk.dotnet clean
dotnet-sdk.dotnet build --configuration:${PROFILE} --runtime:ubuntu.18.04-x64

cd $BASEDIR

echo "Packing Trape Collector"
cd $BASEDIR

# Creating directory structure

VERSION=0.1-1
PACKINGDIR=$BASEDIR/packaging/trape-trader_$VERSION
rm -rf $PACKINGDIR
rm -rf trape-trader_$VERSION.deb
mkdir -p $PACKINGDIR/opt/trape/trape-trader/
mkdir -p $PACKINGDIR/DEBIAN
cp -r $SOURCEDIR/trape/Trape.Cli.Trader/bin/Debug/netcoreapp3.1/ubuntu.18.04-x64/* $PACKINGDIR/opt/trape/trape-trader/
chmod 664 $PACKINGDIR/opt/trape/trape-trader/*
chmod 744 $PACKINGDIR/opt/trape/trape-trader/Trape.Cli.Trader


cat << EOF > $PACKINGDIR/DEBIAN/control
Package: trape-trader
Version: $VERSION
Section: base
Priority: optional
Architecture: x86-64
Depends: 
Maintainer: Damian Wolgast <damian.wolgast@asymetrixs.net>
Description: Trape Trader is the trading bot that consumes data from Trape Collector..
 It connects to binance and reads/writes trading information into the database..
EOF

cd $BASEDIR/packaging
dpkg-deb -v --build trape-trader_$VERSION


