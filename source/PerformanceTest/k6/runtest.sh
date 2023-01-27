curl -X PUT https://localhost:7131/api/ResetActors
k6 run --vus 100 --iterations 100 script.js
