using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using System.Collections.Generic;
using Pulumi.AzureNative.Sql;
using System.IO;
using Pulumi.SyncedFolder;
using AzureNative = Pulumi.AzureNative;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.Network;
using Pulumi.AzureAD;


return await Pulumi.Deployment.RunAsync(() =>
{
    // Create an Azure Resource Group
    var resourceGroup = new ResourceGroup("resourceGroup");
    //var resourceGroupName = "my-resource-group";
    var storageAccountName = "mystorageaccount";
    var appServiceName = "my-app-service";
    var containerName = "my-container";

    // Create an Azure resource (Storage Account)
    var storageAccount = new StorageAccount("sa", new StorageAccountArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Sku = new SkuArgs
        {
            Name = Pulumi.AzureNative.Storage.SkuName.Standard_LRS
        },
        Kind = Kind.StorageV2
    });

    var storageAccountKeys = ListStorageAccountKeys.Invoke(new ListStorageAccountKeysInvokeArgs
    {
        ResourceGroupName = resourceGroup.Name,
        AccountName = storageAccount.Name
    });

    var primaryStorageKey = storageAccountKeys.Apply(accountKeys =>
    {
        var firstKey = accountKeys.Keys[0].Value;
        return Output.CreateSecret(firstKey);
    });
    // Create a container in the storage account for deployment
    var container = new BlobContainer(containerName, new BlobContainerArgs
    {
        AccountName = storageAccount.Name,
        PublicAccess = PublicAccess.Blob,
        ContainerName= containerName,
        ResourceGroupName= resourceGroup.Name
    });
    /*
        // Upload the project files to the container
        var appFiles = Directory.GetFiles("C:\\azuredemo\\www", "*", SearchOption.AllDirectories);
        foreach (var file in appFiles)
        {
            var blob = new Blob($"{container.Name}/{Path.GetFileName(file)}", new BlobArgs
            {
                ResourceGroupName = resourceGroup.Name,
                AccountName = storageAccount.Name,
                ContainerName = container.Name,
                Type = BlobType.Block,
                Source = new FileAsset(file)
            });
        }

        */
    var folder = new AzureBlobFolder("synced-folder", new AzureBlobFolderArgs
    {
        ResourceGroupName = resourceGroup.Name,
        StorageAccountName = storageAccount.Name,
        ContainerName = container.Name,
        Path = "./www",
    });



    // Create an Azure SQL Server.
    var sqlServer = new AzureNative.Sql.Server("mySqlServer", new ServerArgs
            {
                ResourceGroupName = resourceGroup.Name,
                ServerName = "my-sql-server",
                AdministratorLogin = "myadminuser",
                AdministratorLoginPassword = "myAdminPw123@23",
                Location = "North Europe"
             
    });
    
    // Create an Azure SQL Database.
    var sqlDatabase = new Database("mySqlDatabase", new DatabaseArgs
    {
        ResourceGroupName = resourceGroup.Name,
        ServerName = sqlServer.Name,
        DatabaseName = "my-database",
        Location = resourceGroup.Location,
        
    });
    

    /*
    var tenant = new AzureNative.AzureActiveDirectory.B2CTenant("myTenant", new TenantArgs
    {
        DisplayName = "My Tenant",
        IsInitial = false
    });*/


    // Create an Azure Active Directory user.
    var user = new Pulumi.AzureAD.User("myUser", new UserArgs
    {
        DisplayName = "My User",
        MailNickname = "myuser",
        UserPrincipalName = "myuser@mytenant.onmicrosoft.com",
        Password = "SecretP@sswd99!",
        /*
        PasswordProfile = new PasswordProfileArgs
        {
            Password = "MyPassword1!",
            ForceChangePasswordNextLogin = false
        }*/
    });

    // Create an Azure Active Directory group.
    var group = new Pulumi.AzureAD.Group("myGroup", new GroupArgs
    {
        DisplayName = "My Group",
        MailNickname = "mygroup",
        MailEnabled = false,
        SecurityEnabled = true
    });

    // Add the user to the group.
    var member = new GroupMember("myGroupMembership", new GroupMemberArgs
    {
        GroupObjectId = group.ObjectId,
        MemberObjectId = user.ObjectId
    });

    // Create an Azure Active Directory application.
    var app = new Application("myApp", new ApplicationArgs
    {
        DisplayName = "My App"
       
    });

    // Create a service principal for the application.
    var sp = new ServicePrincipal("mySp", new ServicePrincipalArgs
    {
        ApplicationId = app.ApplicationId
    });
    var roleDefinition = new AzureNative.Authorization.RoleDefinition("roleDefinition", new()
    {
        RoleDefinitionId = "roleDefinitionId",
        Scope = "scope",
    });
/*
    // Create a role definition for the service principal.
    var roleDefinition = new RoleDefinition("myRoleDefinition", new RoleDefinitionArgs
    {
        DisplayName = "My Role Definition",
        Description = "My custom role definition.",
        Permissions = new Permission[]
        {
                    new Permission
                    {
                        Actions = new string[] { "Microsoft.Storage/storageAccounts/blobServices/containers/write" },
                        DataActions = new string[] { },
                        NotActions = new string[] { },
                        NotDataActions = new string[] { }
                    }
        },
        AssignableScopes = new string[] { "/" }
    });
*/
    // Export the primary key of the Storage Account
    return new Dictionary<string, object?>
    {
        ["primaryStorageKey"] = primaryStorageKey
    };
});