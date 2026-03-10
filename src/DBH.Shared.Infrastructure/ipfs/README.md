# IPFS with Docker (Upload and Retrieve Files)

This project runs a local IPFS node using Docker.

## Prerequisites

- Docker Desktop installed and running
- PowerShell (Windows)

## 1) Start IPFS

```powershell
docker compose up -d
```

Check logs:

```powershell
docker compose logs -f ipfs
```

You should see `Daemon is ready`.

## 2) Upload a File

Use the helper script:

```powershell
./upload-file.ps1 -FilePath "./myfile.txt"
```

Example output:

- CID: `bafy...`

You can also upload manually:

```powershell
curl.exe -X POST "http://localhost:5001/api/v0/add?pin=true" -F "file=@./myfile.txt"
```

## 3) Retrieve a File

Use the helper script:

```powershell
./retrieve-file.ps1 -Cid "bafy..." -OutFile "./restored.txt"
```

Or retrieve manually from gateway:

```powershell
curl.exe "http://localhost:8080/ipfs/bafy..." -o restored.txt
```

## 4) C# IpfsClient

A C# console application is also available for use.

### Build the client

```powershell
cd IpfsClient
dotnet build
```

### Upload with C#

```powershell
dotnet run -- upload "../myfile.txt"
```

### Retrieve with C#

```powershell
dotnet run -- retrieve "bafy..." "../restored.txt"
```

## 5) Open in Browser

- Gateway: http://localhost:8080
- Web UI: http://localhost:5001/webui

To view a CID in browser:

- http://localhost:8080/ipfs/<CID>

## 5) Stop IPFS

```powershell
docker compose down
```

Data is persisted in:

- `./ipfs_data`

## Quick Test

dotnet run -- upload "test.txt.aes"
dotnet run -- retrieve "Qmb2iMvSd6qfcjzRFoqaQeimWUoj7qev7jVFfhdnqu8MTo"