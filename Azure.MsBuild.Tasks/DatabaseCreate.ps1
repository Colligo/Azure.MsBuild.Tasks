function main($arguments)
{
	$a = handleArgs($arguments);
	
	$PublishSettingsFile = $null;
	$SubscriptionName = $null;
	$WebsiteName = $null;
	$AzureDatabaseServer = $null;
	$DatabaseName = $null;
	$WorkerRoleConfig = $null
	$Slot = $null
	$WorkerRoleName = $null
	$DatabaseAdmin = $null
    $DatabasePassword = $null
	
	if($a.contains("PublishSettingsFile")) { $PublishSettingsFile = $a["PublishSettingsFile"]; }
	if($a.contains("SubscriptionName")) { $SubscriptionName = $a["SubscriptionName"]; }
	if($a.contains("WebsiteName")) { $WebsiteName = $a["WebsiteName"]; }
	if($a.contains("AzureDatabaseServer")) { $AzureDatabaseServer = $a["AzureDatabaseServer"]; }
	if($a.contains("DatabaseName")) { $DatabaseName = $a["DatabaseName"]; }
	if($a.contains("WorkerRoleConfig")) { $WorkerRoleConfig = $a["WorkerRoleConfig"]; }
	if($a.contains("Slot")) { $Slot = $a["Slot"]; }
	if($a.contains("WorkerRoleName")) { $WorkerRoleName = $a["WorkerRoleName"]; }
	if($a.contains("DatabaseAdmin")) { $DatabaseAdmin = $a["DatabaseAdmin"]; }
	if($a.contains("DatabasePassword")) { $DatabasePassword = $a["DatabasePassword"]; }
	
	return CreateDB $SubscriptionName $PublishSettingsFile $WebsiteName $AzureDatabaseServer $DatabaseName $WorkerRoleConfig $Slot $WorkerRoleName $DatabaseAdmin $DatabasePassword;
}


function CreateDB([string]$SubscriptionName, [string]$PublishSettingsFile, [string]$Websiteame, [string]$AzureDatabaseServer, [string]$DatabaseName, [string]$WorkerRoleConfig, [string]$Slot, [string]$WorkerRoleName, [string]$DatabaseAdmin, [string]$DatabasePassword)
{
	write-host PublishSettingsFile $PublishSettingsFile;
	write-host SubscriptionName $SubscriptionName;
	write-host WebsiteName $WebsiteName;
	write-host AzureDatabaseServer $AzureDatabaseServer;
	write-host DatabaseName $DatabaseName;
	write-host WorkerRoleConfig $WorkerRoleConfig;
	write-host Slot $Slot;
	write-host WorkerRoleName $WorkerRoleName;
	write-host DatabaseAdmin $DatabaseAdmin;
	write-host DatabasePassword $DatabasePassword;

	
	Write-Host Importing Azure Publish Settings from  $PublishSettingsFile
	Import-AzurePublishSettingsFile $PublishSettingsFile

	if ( $? -ne "True")
	{
	  throw " Import Publish Settings File failed"
	}

	Write-Host Selecting Subscription $SubscriptionName
	Select-AzureSubscription -SubscriptionName $SubscriptionName

	if ( (Get-AzureWebsite $WebsiteName) -eq $null )
	{
		throw "Azure Website $WebsiteName is null"
	}

	if( (Get-AzureDeployment -ServiceName $WorkerRoleName) -eq $null )
	{
		throw "Azure WorkerRole $WorkerRoleName is null"
	}

	if( $? -eq "True")
	{
	
		Write-Host New ConnectionString: "Server=tcp:$AzureDatabaseServer.database.windows.net; Database=$DatabaseName;User ID=$DatabaseAdmin@$AzureDatabaseServer;Password=$DatabasePassword; Trusted_Connection=False;Encrypt=True;" 
		$connectionString = "Server=tcp:$AzureDatabaseServer.database.windows.net; Database=$DatabaseName;User ID=$DatabaseAdmin@$AzureDatabaseServer;Password=$DatabasePassword; Trusted_Connection=False;Encrypt=True;"
	
		$connectionStringInfo = (`
			@{Name = "Context"; Type = "SQLAzure"; ConnectionString =$connectionString}
		);
		$listOfConnectionStrings = (Get-AzureWebsite $WebsiteName).ConnectionStrings;
		$listOfConnectionStrings.Clear();
		$listOfConnectionStrings.Add($connectionStringInfo);

		Write-Host Setting Website $WebsiteName with new Connection Info $connectionStringInfo;
		Set-AzureWebsite $WebsiteName -ConnectionStrings $listOfConnectionStrings;
	}
	else
	{
		throw  "Error Selecting subscription.  Aborting!"
	}
	
	return $exitCode = 0;
}


function handleArgs($argList) {
	$arguments = @{}
	$counter = 0;
	foreach($a in $argList) {
		if($a -ne $null) {
			if( ($counter%2) -eq 0) {
				$name = $a;
				if($name.StartsWith("-") -eq "True") {
					$name = $name.Substring(1);
				}
				$name=$name.Trim();
			} else {
				if($a -is [int]) {
					$value = $a
				} else {
					$value = $a.Trim();	
				}				
				$arguments.Add($name, $value);
				$name = $null;
			}
		}
		$counter++;
	}
	return $arguments;
}


$a = $args
return main($a);
