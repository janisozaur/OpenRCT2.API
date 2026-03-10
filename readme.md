# OpenRCT2 Public REST API

| Branch      | Status  |
|-------------|---------|
| **master**  | [![AppVeyor](https://ci.appveyor.com/api/projects/status/60n0fpq53ddr0gjh/branch/master?svg=true)](https://ci.appveyor.com/project/OpenRCT2/openrct2-api) |

## Windows / macOS / Linux
See instructions at https://www.microsoft.com/net/core

## Docker
```
cd=`pwd`
docker pull mcr.microsoft.com/dotnet/sdk:10.0
docker run -v "$(pwd)":/work -w /work -it -p 5000:80 mcr.microsoft.com/dotnet/sdk:10.0 bash
```

## Database
```
# in terminal:
docker volume create rethinkdb_data
docker run --rm -it --name rethinkdb-manual -v rethinkdb_data:/data -p 8080:8080 -p 28015:28015 -p 29015:29015 rethinkdb

# in browser:
http://localhost:8080

# go to Data Explorer and run:
r.dbCreate("live")

r.db("rethinkdb").table("users").insert({id: "openrct2-api-live", password: "<PASSWORD>"}, { conflict: "replace" })

r.grant("openrct2-api-live", {"config": true})

# remember to replace <PASSWORD> with a strong password and update api.config.yml accordingly
```

## Building / Launching
```
cd src/OpenRCT2.API
dotnet run
```

## Configuration
~/.openrct2/api.config.yml:
```
api:
  bind:
  baseUrl:
database:
  host:
  user:
  password:
  name:
s3:
  key:
  secret:
  region:
  endpoint:
openrct2.org:
  applicationToken:
```
