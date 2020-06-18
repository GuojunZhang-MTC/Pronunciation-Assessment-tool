# Pronunciation-Assessment-tool
It's based on the Azure Speech Pronunciation Assessment. API makes a tool for experiencing Microsoft Pronunciation Assessment's Capacity. It includes TTS and Pronunciation score, and supports both Chinese and English languages.
Operating environment Visual studios 2017 or later, with .Net Framework 4.0 or later installed Version.


After downloading the code, enter the Azure Speech Key, noting the data center where the endpoint is located.
        private string subscriptionKey = "input your subscriptionKey"; // Replace this with your subscription key 
        private string region = "eastasia";// Replace this with the region corresponding to your subscription key, e.g. westus, eastasia, centralindia 

       private string language = "en-us";
       
    Replace the sentences that need to be scored in the following code. 
     int currentOIndex = 0;

        List<String> examEN = new List<string> { "Try out pronunciation assessment", "play tennis piano ride club term board would like well  all that's all worry worry about  teach then" };
        List<String> examCN = new List<string> { "欢迎来到微软技术中心" };
