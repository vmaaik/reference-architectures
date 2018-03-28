### Known Issues  ###

Issue 1. GitHub has updated to TLS 1.3 and DSC only supports TLS 1.0.  Therefore DSC no longer works on any of the GitHub repository. 

**An update to fix this issue is expected by April 4 2018**

Work around - I have already implemented this work around. So you shouldn't have to do it.
  1. Create an Azure Storage account
  2. Create container in the Blob and give it public access
  3. Upload all teh DSC extension files to that container
  4. In the JSON files - change the URL to point to these files in this public storage instead of GitHub
  

For scripts in larry/sap branch. I have created an Azure Storage account and copied the 2 DSC files up to this account. In addition I updated the URL in the JSON files to point to the these files.


Storage Account: lbsaptest
Resource Group: lbRGSAPStorage 
Location: East US

Here's the location of the DSC files

https://lbsaptest.blob.core.windows.net/nwwindows/adds.zip
https://lbsaptest.blob.core.windows.net/nwwindows/PrepareWSFC.ps1.zip

So far from my testing this is working.  Let me know if you encouter any issues.


### Deploy the solution using azbb

To deploy the SAP NetWeaver Windows  reference architecture, follow these steps:

1. Navigate to the `sap\sap-hana-v2` folder for the repository you cloned in step 1 of the pre-requisites above.

2. Login to Azure 'az login'

3. The parameter file specifies a default adminstrator user name and password for each VM in the deployment. You must change these before you deploy the reference architecture. Open the `sap-hana-v2.json` and 'sap-hana-workload-v2.json' file and replace each **adminUsername** and **adminPassword** field with your new settings.   Save the file.

4. Deploy the 1st part of  reference architecture using the **azbb** command line tool as shown below.

  ```bash
  azbb -s <your subscription_id> -g <your resource_group_name> -l <azure region> -p sap-hana-v2.json --deploy
  ```

5. Deploy the 2nd part of  reference architecture using the **azbb** command line tool as shown below.

  ```bash
  azbb -s <your subscription_id> -g <your resource_group_name> -l <azure region> -p sap-hana-workload-v2.json --deploy
  ```


For more information on deploying this sample reference architecture using Azure Building Blocks, visit the [GitHub repository][git].

