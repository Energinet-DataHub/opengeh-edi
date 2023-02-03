rm -rf stats-report-output
curl -X PUT https://localhost:7131/api/ResetActors
jmeter -n -t PeekDequeue.jmx -l results.txt -e -o stats-report-output
mv results.txt results-$(date +"%Y-%m-%d_%H:%M").txt

