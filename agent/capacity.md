# This document explains how to add a capacity to a running agent with ADO API

There are 3 API to call in order to be able to programmatically and dynamically add capacity to an agent:

1. Gets the list of pools to find the pool ID
1. Gets the list of agents to find the agent ID
1. Put the capacities into the agent ID

ADO works with Personal Access Token (PAT) token. Authentication is then set to `Basic` with a base 64 encoded empty user with the PAT token as password. [Explanations can be found here](https://learn.microsoft.com/en-us/rest/api/azure/devops/?view=azure-devops-rest-7.2#assemble-the-request).

## Getting the pools

The [API definition is here](https://learn.microsoft.com/en-us/rest/api/azure/devops/distributedtask/pools/get-agent-pools?view=azure-devops-rest-7.2). There is a way to filter by pool name.

```http
GET https://dev.azure.com/nanoframework/_apis/distributedtask/pools?poolName=TestStream&api-version=7.2-preview.1
Authorization: Basic OnE1dWxweGt1NmVlc3NxeWhtYnB0ZnR4eHh2ZWU1enZpZnVmbnY0N2Vjd25nMzNvZDU0eHE=
Accept:application/json;
```

The return JSON looks like:

```json
{
  "count": 1,
  "value": [
    {
      "createdOn": "2024-08-25T15:04:29.31Z",
      "autoProvision": false,
      "autoUpdate": true,
      "autoSize": true,
      "targetSize": null,
      "agentCloudId": null,
      "createdBy": {
        "displayName": "Laurent Ellerbach",
        "url": "https://spsprodweu3.vssps.visualstudio.com/Aea857fe6-678b-4b29-9ea7-843b68db2b04/_apis/Identities/e1ebc4a3-573d-473b-bad1-780f2aa40667",
        "_links": {
          "avatar": {
            "href": "https://dev.azure.com/nanoframework/_apis/GraphProfile/MemberAvatars/aad.MDIzYWYwMjMtMTdhNS03OWRkLTliZDYtNDYzYWQ0MDE5YTMy"
          }
        },
        "id": "e1ebc4a3-573d-473b-bad1-780f2aa40667",
        "uniqueName": "laurelle@microsoft.com",
        "imageUrl": "https://dev.azure.com/nanoframework/_apis/GraphProfile/MemberAvatars/aad.MDIzYWYwMjMtMTdhNS03OWRkLTliZDYtNDYzYWQ0MDE5YTMy",
        "descriptor": "aad.MDIzYWYwMjMtMTdhNS03OWRkLTliZDYtNDYzYWQ0MDE5YTMy"
      },
      "owner": {
        "displayName": "Laurent Ellerbach",
        "url": "https://spsprodweu3.vssps.visualstudio.com/Aea857fe6-678b-4b29-9ea7-843b68db2b04/_apis/Identities/e1ebc4a3-573d-473b-bad1-780f2aa40667",
        "_links": {
          "avatar": {
            "href": "https://dev.azure.com/nanoframework/_apis/GraphProfile/MemberAvatars/aad.MDIzYWYwMjMtMTdhNS03OWRkLTliZDYtNDYzYWQ0MDE5YTMy"
          }
        },
        "id": "e1ebc4a3-573d-473b-bad1-780f2aa40667",
        "uniqueName": "laurelle@microsoft.com",
        "imageUrl": "https://dev.azure.com/nanoframework/_apis/GraphProfile/MemberAvatars/aad.MDIzYWYwMjMtMTdhNS03OWRkLTliZDYtNDYzYWQ0MDE5YTMy",
        "descriptor": "aad.MDIzYWYwMjMtMTdhNS03OWRkLTliZDYtNDYzYWQ0MDE5YTMy"
      },
      "id": 11,
      "scope": "f3e49312-780d-452a-9d3d-bf7cf9af8b7e",
      "name": "TestStream",
      "isHosted": false,
      "poolType": "automation",
      "size": 2,
      "isLegacy": false,
      "options": "none"
    }
  ]
}
```

In this case, we're interested into `"id": 11`, so `value[0].id`.

## Getting the agent id

Similar to the previous API, this one lists all the agents attached to the pool ID. [Documentation can be found here](https://learn.microsoft.com/en-us/rest/api/azure/devops/distributedtask/agents/list?view=azure-devops-rest-7.2).

```http
GET https://dev.azure.com/nanoframework/_apis/distributedtask/pools/11/agents?agentName=Ellerbach-N100&api-version=7.2-preview.1
Authorization: Basic OnE1dWxweGt1NmVlc3NxeWhtYnB0ZnR4eHh2ZWU1enZpZnVmbnY0N2Vjd25nMzNvZDU0eHE=
Accept:application/json;
```

The returned JSON looks like:

```json
{
  "count": 1,
  "value": [
    {
      "_links": {
        "self": {
          "href": "https://dev.azure.com/nanoframework/_apis/distributedtask/pools/11/agents/99"
        },
        "web": {
          "href": "https://dev.azure.com/nanoframework/_settings/agentpools?view=jobs&poolId=11&agentId=99"
        }
      },
      "maxParallelism": 1,
      "createdOn": "2024-09-16T18:22:51.053Z",
      "statusChangedOn": "2024-09-16T18:23:06.217Z",
      "authorization": {
        "clientId": "e1fe43b6-c41e-413c-ba7f-6e93bf48a4c6",
        "publicKey": {
          "exponent": "AQAB",
          "modulus": "syhCQ99LW6Ak6hIYJ1ZZXRcmUgFN2KmlWqj+/TFf9FCYTjpi73T14GZ07RdpZUlNCd4s5zGq8pLK2zyr9B2i181KJxDoSS25yiPWHQF8gVJipES9Bp5eE+q3uqYpr79bgEnTBWrFuLgVCADbiPF4Wd4SeOBTVe2/zItpA3leTAevI607UDzaH++JZbd13XGncSeQRMJV4bxlkIe2GC1aW90SvidBuibgTsBxRc5oxwP34xfaTFHzM9LiSlAEPusHgEEQ/KdAOKzNu3CPzinR/gn98UxnMxClM08hnns3m6LB3x6czjtRoOG2666icRVSBw9BZgWwPTJjH/uBvISjfw=="
        }
      },
      "id": 99,
      "name": "Ellerbach-N100",
      "version": "3.243.1",
      "osDescription": "Linux 5.15.153.1-microsoft-standard-WSL2 #1 SMP Fri Mar 29 23:14:13 UTC 2024",
      "enabled": true,
      "status": "online",
      "provisioningState": "Provisioned",
      "accessPoint": "CodexAccessMapping"
    }
  ]
}
```

In this case, the id is `"id": 99` which is `value[0].id`.

> [!Important]
> You need to ensure a uniqueness of the names for the agent so that, you only have one return value.

## Setting the user capabilities for a specific agent

All custom properties can be set individually. [See the documentation here](https://learn.microsoft.com/en-us/rest/api/azure/devops/distributedtask/agents/update?view=azure-devops-rest-7.2).

```http
PUT https://dev.azure.com/nanoframework/_apis/distributedtask/pools/11/agents/99/usercapabilities?api-version=7.2-preview.1
Authorization: Basic OnE1dWxweGt1NmVlc3NxeWhtYnB0ZnR4eHh2ZWU1enZpZnVmbnY0N2Vjd25nMzNvZDU0eHE=
Content-Type: application/json
Accept: application/json

{
    "ESP32": "ESP32",
    "ESP32-C3": "ESP32-C3"
}
```

And the answer will be a JSON containing all the data associated to the agent which will include the user capabilities set.
