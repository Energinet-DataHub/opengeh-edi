// run 100 virtual users in paralle
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

    let actorToken = actorTokenResponse.body;

    let headers = {
      authorization: `Bearer ${actorToken}`,
    };

    const peekResponse = http.get(
      `${messagingApiHostDns}:${messagingApiHostPort}/api/peek/masterdata`,
      {
        headers: headers,
      }
    );

    const noContent =
      peekResponse.status === 204 || peekResponse.status === 500;

    if (noContent) return;

    const MessageId = peekResponse.headers.MessageId;
    http.del(
      `${messagingApiHostDns}:${messagingApiHostPort}/api/dequeue/${MessageId}`,
      {
        headers: headers,
      }
    );
  }
}
