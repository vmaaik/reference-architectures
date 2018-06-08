

function SetGlobalParams  {

    # dictionary object to load varaibles that will be input to asa job
    $global:dict  =  Get-Content .\asa_job_inputs.json  | ConvertFrom-Json


}

function UpdateAsaParams {

    $hashTable = $global:dict.asajobinputs 
    $hashTable.GetEnumerator() | ForEach-Object {

        $nameToReplace = -join("<",$_.Name,">")
        (Get-Content .\asa_job.json) -replace  $nameToReplace , $_.Value | 
        Set-Content .\asa_job.json
    }
   
}

SetGlobalParams
UpdateAsaParams