# FinanceBot
A finance chatbot developed using Microsoft Bot Framework and LUIS. 

Issues:
1. Using the API account key instead of LUIS programmatic key. 401 error (permission denied)
Fix: use the starter key provided in LUIS app manage section. Azure keys not working to call the REST APIs.
You have to use the programmatic api to manage the app and the Azure key to query the app.
(Issue github page: https://github.com/Microsoft/Cognitive-LUIS-Windows/issues/23)
(Solution page: https://blogs.msdn.microsoft.com/kwill/2017/05/17/http-401-access-denied-when-calling-azure-cognitive-services-apis/)
