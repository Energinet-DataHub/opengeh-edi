curl -X PUT https://localhost:7131/api/ResetActors
k6 run --vus 20 --iterations 20 script.js
