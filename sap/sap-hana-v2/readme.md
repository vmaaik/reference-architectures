
### Deploy the solution using azbb

To deploy the SAP NetWeaver Windows  reference architecture, follow these steps:

1. Navigate to the `sap\sap-hana-v2` folder for the repository you cloned in step 1 of the pre-requisites above.

2. The parameter file specifies a default adminstrator user name and password for each VM in the deployment. You must change these before you deploy the reference architecture. Open the `sap-hana-v2.json` and 'sap-hana-workload-v2.json' file and replace each **adminUsername** and **adminPassword** field with your new settings.   Save the file.

3. Deploy the 1st part of  reference architecture using the **azbb** command line tool as shown below.

  ```bash
  azbb -s <your subscription_id> -g <your resource_group_name> -l <azure region> -p sap-hana-v2.json --deploy
  ```

4. Deploy the 2nd part of  reference architecture using the **azbb** command line tool as shown below.

  ```bash
  azbb -s <your subscription_id> -g <your resource_group_name> -l <azure region> -p sap-hana-workload-v2.json --deploy
  ```


For more information on deploying this sample reference architecture using Azure Building Blocks, visit the [GitHub repository][git].

