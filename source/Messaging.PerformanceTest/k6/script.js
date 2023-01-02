// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// run 100 virtual users in paralle:wq
// k6 run --vus 100 --iterations 100 script.js

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

  let numberOfMessagesLeft;
  let isGettingMessages = true;
  let loopCount = 0;
  const numberOfMessagesToPeek = NumberOfMessagesLeft(headers);
  console.info(`Ready to Peek! GLN ${actorGln}: Number of messages: ${numberOfMessagesToPeek}`);

  while (isGettingMessages) {
      loopCount++;

      const peekResponse = http.get(
        `${messagingApiHostDns}:${messagingApiHostPort}/api/peek/masterdata`,
        {
          headers: headers,
        }
      );
      
      if (peekResponse.status === 500 || peekResponse.status === 0) {
        console.info(`GLN ${actorGln}: peekResponse.status === 500 || peekResponse.status === 0:  ${peekResponse.status}`);
        continue;
      }

      const noContent = peekResponse.status === 204;
      if (noContent) {
        const messagesLeft = NumberOfMessagesLeft(headers);
        if(messagesLeft > 0) 
        {
          console.info(`NoContent with additional messages: GLN ${actorGln}: Messages left: ${messagesLeft}. Sleep for 1 second and continue the peek/dequeue process.`);
          continue;
        }
        isGettingMessages = false;
        break;
      }

      const MessageId = peekResponse.headers.Messageid;
      if ( MessageId === undefined) {
          console.info(`MessageId error: GLN ${actorGln}: Missing MessageId in peekResponse:  ${JSON.stringify(peekResponse)}`);
          continue;
      }

      console.info(`HTTP delete: GLN ${actorGln}: ${messagingApiHostDns}:${messagingApiHostPort}/api/dequeue/${MessageId}`);
      const dequeueResponse = http.del(
        `${messagingApiHostDns}:${messagingApiHostPort}/api/dequeue/${MessageId}`, null,
        {
          headers: headers,
        }
      );

      numberOfMessagesLeft = NumberOfMessagesLeft(headers);
      if(numberOfMessagesLeft > 0) console.info(`GLN ${actorGln}: Continue peek/dequeue. NumberOfMessagesLeft: ${numberOfMessagesLeft}`);
    }
    numberOfMessagesLeft = NumberOfMessagesLeft(headers);
    console.info(`Peek/Queue DONE! GLN ${actorGln}: LoopCount: ${loopCount} Messages Dequeued: ${numberOfMessagesToPeek} NumberOfMessagesLeft: ${numberOfMessagesLeft}`);
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
  return "unknown";
}