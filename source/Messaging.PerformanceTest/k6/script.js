// run 100 virtual users in paralle:wq
// k6 run --vus 100 --iterations 100 script.js

import { sleep } from 'k6';
import http from 'k6/http';

const messagingApiHostDns = 'http://localhost';
const messagingApiHostPort = '7071';

const performanceTestHostDns = 'https://localhost';
const performanceTestHostPort = '7131';

export default function () {
  const actorNumberResponse = http.get(
    `${performanceTestHostDns}:${performanceTestHostPort}/api/ActorNumber`
  );

  if (actorNumberResponse.status !== 204) {
    const actorTokenResponse = http.get(
      `${performanceTestHostDns}:${performanceTestHostPort}/api/ActorToken/${actorNumberResponse.body}`
    );

    let actorGln = actorNumberResponse.body;
    let actorToken = actorTokenResponse.body;

    console.info(`GLN ${actorGln}: actorToken: ${actorToken}`);

    let headers = {
      'Authorization': 'Bearer ' + actorToken
   };

    let isGettingMessages = true;
    while (isGettingMessages) {
        const peekResponse = http.get(
          `${messagingApiHostDns}:${messagingApiHostPort}/api/peek/masterdata`,
          {
            headers: headers,
          }
        );

        console.info(`Peek response status: ${peekResponse.status}`);

        const MessageId = peekResponse.headers.Messageid;
        if ( MessageId === undefined) {
            console.info(`GLN ${actorGln}: Missing MessageId in peekResponse:  ${JSON.stringify(peekResponse)}`);
        }

        if (peekResponse.status === 500) {
          console.info(`GLN ${actorGln}: sleep for 1 second. internalError: ${peekResponse.status}`);
          sleep(1000);
          continue;
        }

        const noContent = peekResponse.status === 204;

        if (noContent) {
          console.info(`GLN ${actorGln}: noContent: exit loop:  ${peekResponse.status}`);
          isGettingMessages = false;
          break;
        }        

        console.info(`GLN ${actorGln}: http delete: ${messagingApiHostDns}:${messagingApiHostPort}/api/dequeue/${MessageId}`);
        console.info(`GLN ${actorGln}: Headers: ${JSON.stringify(headers)}`);

        const dequeueResponse = http.del(
          `${messagingApiHostDns}:${messagingApiHostPort}/api/dequeue/${MessageId}`, null,
          {
            headers: headers,
          }
        );

        console.info(`GLN ${actorGln}: dequeueResponse status: ${dequeueResponse.status}`);
      }
    }
}
