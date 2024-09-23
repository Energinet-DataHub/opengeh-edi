
# What are the files purpose

- nswag.json | This file is used to generate the client code in WholesaleClient.cs from a swagger.json file located at the url specified in the file.
- swagger.json | This is a copy of the swagger.json file used to generate the client code. This is so we can see what the swagger.json the client code was generated from looked like at the time of generation.
- B2CEdiClient.cs | This is the generated client.

# How to generate the client code

1. Run B2CWebApi.
2. Copy the content of the swagger.json (from Swagger website) to the local swagger.json file.
3. Rebuild the AcceptanceTests project.
4. Manually re-add the license header to the B2CEdiClient.cs file.

The B2CEdiClient.cs file will be updated in step 3.
The ASP.NET Core 6.0 RunTime might be needed.

# How to build the client from a different swagger.json file, than the one specified in nswag.json

Simply go to the nswag.json file and change the url to the swagger.json file you want to use.

# How is the nswag.json file run to generate the client code

In the AcceptanceTests.csproj file there is a target that runs the nswag.json file. Look for the element `<Target Name="NSwag" ...>`.

# Breaking changes to the client code

We need to find out what to do in case of breaking changes to the client code and what impact it will have on the BFF.
