az acr login --name acrlab007hsouza --resource-group LAB007


docker tag bff-rent-car-local acrlab007hsouza.azurecr.io/bff-rent-car-local:v1
docker push acrlab007hsouza.azurecr.io/bff-rent-car-local:v1

az container env create --name bff-rent-car-local --resource-group LAB007 --location eastus

az container create --name bff-rent-car-local --resource-group LAB007 --image acrlab007hsouza.azurecr.io/bff-rent-car-local:v1 --cpu 1 --memory 1.5 --registry-login-server acrlab007hsouza.azurecr.io --registry-username <username> --registry-password <password> --ports 80

#em uma linha só
az containerapp create --name bff-rent-a-car-local 
--resource-group LAB007 
--environment bff-rent-car-local 
--image acrlab007hsouza.azurecr.io/bff-rent-car-local:v1 
--ingress 'external' 
--target-port 5001 
--registry-server acrlab007hsouza.azurecr.io 
--registry-username <username> --registry-password <password>