echo Restoring dotnet tools...
dotnet tool restore

dotnet build src/FSharpy.TaskSeq.sln -c Release