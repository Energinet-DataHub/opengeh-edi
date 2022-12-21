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
    let headers = {
      'Authorization': 'Bearer ' + actorToken
   };

    let isGettingMessages = true;
    while (isGettingMessages) {
        const numberOfMessagesLeft = NumberOfMessagesLeft(headers);
        console.info(`GLN ${actorGln}: NumberOfMessagesLeft: ${numberOfMessagesLeft}`);
        const peekResponse = http.get(
          `${messagingApiHostDns}:${messagingApiHostPort}/api/peek/masterdata`,
          {
            headers: headers,
          }
        );
        
        if (peekResponse.status === 500) {
          console.info(`GLN ${actorGln}: sleep for 1 second. internalError: ${peekResponse.status}`);
          sleep(1000);
          continue;
        }

        const noContent = peekResponse.status === 204;

        if (noContent) {

          const messageCountResponse = http.get(
            `${messagingApiHostDns}:${messagingApiHostPort}/api/messagecount`,
            {
              headers: headers,
            }
          );

          if (messageCountResponse.status === 200) {
            if (messageCountResponse.body > 0) {
              console.info(`GLN ${actorGln}: Addtional messages found: ${messageCountResponse.body}. Sleep for 1 second and continue the peek/dequeue process.`);
              sleep(1000);
              continue;
            }
          }

          console.info(`GLN ${actorGln}: noContent: exit loop:  ${peekResponse.status}`);
          isGettingMessages = false;
          break;
        }        

        const MessageId = peekResponse.headers.Messageid;
        if ( MessageId === undefined) {
            console.info(`GLN ${actorGln}: Missing MessageId in peekResponse:  ${JSON.stringify(peekResponse)}`);
            continue;
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
      const numberOfMessagesLeft = NumberOfMessagesLeft(headers);
        console.info(`DONE! GLN ${actorGln}: NumberOfMessagesLeft: ${numberOfMessagesLeft}`);
    }
}

function NumberOfMessagesLeft(headers) {
  const messageCountResponse = http.get(
    `${messagingApiHostDns}:${messagingApiHostPort}/api/messagecount`,
    {
      headers: headers,
    }
  );

  if (messageCountResponse.status === 200) {
    return messageCountResponse.body;
  }

  return 0;
}

