#!/usr/bin/env bash

dotnet run -- --urls=http://+:9000 --source=rtsp --rtsp:streamurl=rtsp://camera/stream

while : ; do
  sleep 120
done
