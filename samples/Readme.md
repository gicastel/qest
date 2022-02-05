### How to run the provided samples

First things first, you have to build the CLI.
From this directory:
'''
dotnet build ..\src\TestRunner\.
dotnet publish ..\src\TestRunner\. -o .  --runtime linux-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true
'''

Then, build the database project:
'''
dotnet build .\sampleDb\ -o .\dacpac\
'''

Now you are ready to start the testing!
Just run:
'''
docker build .
'''
and everythig works! Everything runs smooth and we have now tested the DB.