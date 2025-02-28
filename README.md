[![build](https://github.com/azure-devops-compliance/azure-functions/workflows/test/badge.svg)](https://github.com/azure-devops-compliance/azure-functions/actions)
[![codecov](https://codecov.io/gh/azure-devops-compliance/azure-functions/branch/master/graph/badge.svg)](https://codecov.io/gh/azure-devops-compliance/azure-functions)
[![stryker](https://img.shields.io/endpoint?style=flat&url=https%3A%2F%2Fbadge-api.stryker-mutator.io%2Fgithub.com%2Fazure-devops-compliance%2Fazure-functions%2Fmaster)](https://dashboard.stryker-mutator.io/reports/github.com/azure-devops-compliance/azure-functions/master)

# Azure DevOps Compliance - Azure Functions

Here is the home of the azure functions that evaluate projects in your Azure DevOps organization.

## Deploy the latest release

shell:
```bash
export azdev_token=
export azdev_organization=
export extension_secret=
export extension_name=azure-devops-compliance
export extension_publisher=riezebosch

export rg=
export name=

az group create --name $rg --location westeurope
az storage account create -g $rg -n $name

function=$(az functionapp create -g $rg -n $name -s $name --consumption-plan-location westeurope --os-type windows --runtime dotnet-isolated --functions-version 4 -o tsv --query "id")
az ad sp create-for-rbac --name $name --role contributor --scopes $function --sdk-auth

az functionapp cors add -a https://$extension_publisher.gallerycdn.vsassets.io -n $name -g $rg
az functionapp config appsettings set --name $name --resource-group $rg --settings TOKEN=$azdev_token ORGANIZATION=$azdev_organization
az functionapp config appsettings set --name $name --resource-group $rg --settings EXTENSION_NAME=$extension_name EXTENSION_PUBLISHER=$extension_publisher EXTENSION_SECRET=$extension_secret  

curl https://github.com/azure-devops-compliance/azure-functions/releases/latest/download/release.zip -L --output release.zip
az functionapp deployment source config-zip -g $rg -n $name --src release.zip
```

powershell: 
```powershell
function Install-AzDevComplianceFunction {
    Param(
        [Parameter(Mandatory=$true)][String] $azdev_token,
        [Parameter(Mandatory=$true)][String] $azdev_organization,
        [Parameter(Mandatory=$true)][String] $extension_secret,
        [Parameter(Mandatory=$false)][String] $extension_name = "azure-devops-compliance",
        [Parameter(Mandatory=$false)][String] $extension_publisher= "riezebosch",
        
        [Parameter(Mandatory=$true)][String] $rg,
        [Parameter(Mandatory=$true)][String] $name
    )

    az group create --name $rg --location westeurope
    az storage account create -g $rg -n $name
    
    $function = az functionapp create -g $rg -n $name -s $name --consumption-plan-location westeurope --os-type windows --runtime dotnet-isolated --functions-version 4 -o tsv --query "id"
    az ad sp create-for-rbac --name $name --role contributor --scopes $function --sdk-auth
    
    az functionapp cors add -a https://$extension_publisher.gallerycdn.vsassets.io -n $name -g $rg
    az functionapp config appsettings set --name $name --resource-group $rg --settings TOKEN=$azdev_token ORGANIZATION=$azdev_organization
    az functionapp config appsettings set --name $name --resource-group $rg --settings EXTENSION_NAME=$extension_name EXTENSION_PUBLISHER=$extension_publisher EXTENSION_SECRET=$extension_secret  

    iwr https://github.com/azure-devops-compliance/azure-functions/releases/latest/download/release.zip -OutFile release.zip
    az functionapp deployment source config-zip -g $rg -n $name --src release.zip
}
```

Drop this function into your terminal and invoke it with the mandatory parameters afterwards.
