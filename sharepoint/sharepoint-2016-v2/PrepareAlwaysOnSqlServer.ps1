Configuration PrepareAlwaysOnSqlServer
{
    param
    (
        [Parameter(Mandatory)]
        [String]$DomainName,

        [Parameter(Mandatory)]
        [System.Management.Automation.PSCredential]$Admincreds,

        [Parameter(Mandatory)]
        [System.Management.Automation.PSCredential]$SQLServicecreds,

        [System.Management.Automation.PSCredential]$SQLAuthCreds,

        [Parameter(Mandatory)]
        [String]$AOEndpointName,

        [UInt32]$DatabaseEnginePort = 1433,

        [Parameter(Mandatory)]
        [UInt32]$NumberOfDisks,

        [Parameter(Mandatory)]
        [String]$WorkloadType,

        [String]$DomainNetbiosName=(Get-NetBIOSName -DomainName $DomainName),

        [Int]$RetryCount=20,
        [Int]$RetryIntervalSec=30
    )

    Import-DscResource -ModuleName xComputerManagement, xSQLServer, xActiveDirectory, xStorage, xNetworking, xPendingReboot

    [System.Management.Automation.PSCredential]$DomainCreds = New-Object System.Management.Automation.PSCredential ("${DomainNetbiosName}\$($Admincreds.UserName)", $Admincreds.Password)
    [System.Management.Automation.PSCredential]$DomainFQDNCreds = New-Object System.Management.Automation.PSCredential ("${DomainName}\$($Admincreds.UserName)", $Admincreds.Password)
    [System.Management.Automation.PSCredential]$SQLCreds = New-Object System.Management.Automation.PSCredential ("${DomainNetbiosName}\$($SQLServicecreds.UserName)", $SQLServicecreds.Password)

    $RebootVirtualMachine = $false

    if ($DomainName)
    {
        $RebootVirtualMachine = $true
    }

    #Finding the next avaiable disk letter for Add disk
    $NewDiskLetter = ls function:[f-z]: -n | ?{ !(test-path $_) } | select -First 1 

    $NextAvailableDiskLetter = $NewDiskLetter[0]
    
    WaitForSqlSetup

    Node localhost
    {
        LocalConfigurationManager
        {            
            ActionAfterReboot = 'ContinueConfiguration'            
            ConfigurationMode = 'ApplyOnly'            
            RebootNodeIfNeeded = $true            
        }

        WindowsFeature AddFailoverFeature
        {
            Ensure = 'Present'
            Name   = 'Failover-clustering'
        }

        WindowsFeature FailoverClusterTools 
        { 
            Ensure = "Present" 
            Name = "RSAT-Clustering-Mgmt"
            DependsOn = "[WindowsFeature]AddFailoverFeature"
        } 

        WindowsFeature AddRemoteServerAdministrationToolsClusteringPowerShellFeature
        {
            Ensure    = 'Present'
            Name      = 'RSAT-Clustering-PowerShell'
            DependsOn = '[WindowsFeature]AddFailoverFeature'
        }

        WindowsFeature ADPS
        {
            Name = "RSAT-AD-PowerShell"
            IncludeAllSubFeature = $true
            Ensure = "Present"
        }

        WindowsFeature AddRemoteServerAdministrationToolsClusteringCmdInterfaceFeature
        {
            Ensure    = 'Present'
            Name      = 'RSAT-Clustering-CmdInterface'
            DependsOn = '[WindowsFeature]AddRemoteServerAdministrationToolsClusteringPowerShellFeature'
        }

        xWaitForADDomain DscForestWait 
        { 
            DomainName = $DomainName 
            DomainUserCredential= $DomainCreds
            RetryCount = $RetryCount 
            RetryIntervalSec = $RetryIntervalSec 
	        DependsOn = "[WindowsFeature]AddRemoteServerAdministrationToolsClusteringCmdInterfaceFeature"
        }
        
        xComputer DomainJoin
        {
            Name = $env:COMPUTERNAME
            DomainName = $DomainName
            Credential = $DomainCreds
	        DependsOn = "[xWaitForADDomain]DscForestWait"
        }

        xFirewall DatabaseEngineFirewallRule
        {
            Direction = "Inbound"
            Name = "SQL-Server-Database-Engine-TCP-In"
            DisplayName = "SQL Server Database Engine (TCP-In)"
            Description = "Inbound rule for SQL Server to allow TCP traffic for the Database Engine."
            Group = "SQL Server"
            Enabled = $true
            Action = 'Allow'
            Protocol = "TCP"
            LocalPort = $DatabaseEnginePort -as [String]
            Ensure = "Present"
        }

        xFirewall DatabaseMirroringFirewallRule
        {
            Direction = "Inbound"
            Name = "SQL-Server-Database-Mirroring-TCP-In"
            DisplayName = "SQL Server Database Mirroring (TCP-In)"
            Description = "Inbound rule for SQL Server to allow TCP traffic for the Database Mirroring."
            Group = "SQL Server"
            Enabled = $true
            Action = 'Allow'
            Protocol = "TCP"
            LocalPort = "5022"
            Ensure = "Present"
        }

        xFirewall ListenerFirewallRule
        {
            Direction = "Inbound"
            Name = "SQL-Server-Availability-Group-Listener-TCP-In"
            DisplayName = "SQL Server Availability Group Listener (TCP-In)"
            Description = "Inbound rule for SQL Server to allow TCP traffic for the Availability Group listener."
            Group = "SQL Server"
            Enabled = $true
            Action = 'Allow'
            Protocol = "TCP"
            LocalPort = "59999"
            Ensure = "Present"
        }

        xSQLServerLogin AddDomainAdminAccountToSysadminServerRole
        {
            Ensure               = 'Present'
            Name                 = $DomainCreds.UserName
            LoginType            = 'WindowsUser'
            SQLServer            = $env:ComputerName
            SQLInstanceName      = "MSSQLSERVER"
            PsDscRunAsCredential = $Admincreds
        }

        xADUser CreateSqlServerServiceAccount
        {
            DomainAdministratorCredential = $DomainCreds
            DomainName = $DomainName
            UserName = $SQLServicecreds.UserName
            Password = $SQLServicecreds
            Ensure = "Present"
            DependsOn = "[xSQLServerLogin]AddDomainAdminAccountToSysadminServerRole"
        }

        xSQLServerLogin AddSqlServerServiceAccountToSysadminServerRole
        {
            Ensure = 'Present'
            Name = $SQLCreds.UserName
            LoginType = "WindowsUser"
            SQLServer            = $env:ComputerName
            SQLInstanceName      = "MSSQLSERVER"           
            PsDscRunAsCredential = $Admincreds
            DependsOn = "[xADUser]CreateSqlServerServiceAccount"
        }
        
        # xSQLServerAlwaysOnService ConfigureSqlServerSecondaryWithAlwaysOn
        # {
        #     Ensure               = 'Present'
        #     SQLServer            = $env:ComputerName
        #     SQLInstanceName      = 'MSSQLSERVER'
        #     RestartTimeout       = 120
        #     PsDscRunAsCredential = $Admincreds
        #     DependsOn = "[xSQLServerLogin]AddSqlServerServiceAccountToSysadminServerRole"
        # }
        
        # xSQLServerEndpoint SqlSecondaryAlwaysOnEndpoint
        # {
        #     SQLServer            = $env:ComputerName
        #     SQLInstanceName      = 'MSSQLSERVER'
        #     EndpointName         = $AOEndpointName
        #     Port                 = 5022
        #     PsDscRunAsCredential = $Admincreds
        #     DependsOn ="[xSQLServerAlwaysOnService]ConfigureSqlServerSecondaryWithAlwaysOn"
        # }

        xPendingReboot Reboot1
        { 
            Name = "RebootServer"
            DependsOn = "[xSQLServerLogin]AddSqlServerServiceAccountToSysadminServerRole"
        }        
    }
}

function Get-NetBIOSName
{ 
    [OutputType([string])]
    param(
        [string]$DomainName
    )

    if ($DomainName.Contains('.')) {
        $length=$DomainName.IndexOf('.')
        if ( $length -ge 16) {
            $length=15
        }
        return $DomainName.Substring(0,$length)
    }
    else {
        if ($DomainName.Length -gt 15) {
            return $DomainName.Substring(0,15)
        }
        else {
            return $DomainName
        }
    }
}
function WaitForSqlSetup
{
    # Wait for SQL Server Setup to finish before proceeding.
    while ($true)
    {
        try
        {
            Get-ScheduledTaskInfo "\ConfigureSqlImageTasks\RunConfigureImage" -ErrorAction Stop
            Start-Sleep -Seconds 5
        }
        catch
        {
            break
        }
    }
}
