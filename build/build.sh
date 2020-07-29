#!/bin/bash

BASEDIR=$(readlink -f $(dirname $0))
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
	PROFILE=d
elif [[ "d" == "$PROFILE" ]]; then
	PROFILE=d
elif [[ "r" == "$PROFILE" ]]; then
	PROFILE=r
else
	echo "Provided argument '$1' was not understood"
	exit 1;
fi

echo "Building Profile: $PROFILE"

echo "Building Trape Collector"
$BASEDIR/build-trape-collector $PROFILE

echo "Building Trape Trader"
$BASEDIR/build-trape-trader $PROFILE


