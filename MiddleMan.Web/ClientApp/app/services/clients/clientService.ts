import { env } from "~/environment";
import { DELETE, GET, POST, POST_RAW } from "../requests/requests";
import type {
  Client,
  ClientName,
  ClientTokenData,
  ClientWithMethods,
} from "../../contracts/client";
import type { ClientMethod } from "../../contracts/clientMethods";
import { mapClientMethod } from "~/mappers/clientMethodMapper";

export async function createClient(client: ClientName): Promise<Client | null> {
  const result = await POST(`${env.API_BASE_URL}/clients`, client);
  return await result?.json();
}

export async function getClients(): Promise<Client[]> {
  const result = await GET(`${env.API_BASE_URL}/clients`);

  try {
    const data = ((await result?.json()) ?? []) as any[];

    const clients = data.map((d) => {
      return {
        name: d.name,
        methodsUrl: d.methodsUrl,
        isConnected: d.isConnected,
        signature: d.signature,
        tokenHash: d.tokenHash,
      };
    });

    clients.sort((a, b) => (a.name > b.name ? 1 : -1));

    return clients;
  } catch (error) {
    console.error("Error parsing JSON response:", error);
  }

  return [];
}

export async function deleteClient(client: ClientName): Promise<boolean> {
  const response = await DELETE(`${env.API_BASE_URL}/clients/${client.name}`);
  return response != null;
}

export async function refreshClientToken(
  client: ClientName
): Promise<ClientTokenData | null> {
  const response = await POST(
    `${env.API_BASE_URL}/clients/${client.name}/token`,
    null
  );
  if (!response) return null;
  return await response.json();
}

export async function deleteClientToken(
  client: ClientName
): Promise<ClientTokenData | null> {
  const response = await DELETE(
    `${env.API_BASE_URL}/clients/${client.name}/token`
  );
  if (!response) return null;
  return await response.json();
}

export async function callClientMethod(
  client: string,
  method: string,
  isBinaryData: boolean,
  receivesBineryData: boolean,
  data: any
): Promise<any | null> {
  const formattedData: any = [];
  let result: Response | null = null;
  
  if (isBinaryData) {
    result = await POST_RAW(
      `${env.API_BASE_URL}/websockets/${client}/${method}`,
      data
    );  
  } else {
    if (data) {
      Object.keys(data).forEach((key) => {
        formattedData.push(data[key]);
      });
    }

    result = await POST(
      `${env.API_BASE_URL}/websockets/${client}/${method}`,
      formattedData
    );
  }
  
  if (!result) return null;
  if (receivesBineryData) {
    const blob = await result.blob();
    const url = URL.createObjectURL(blob);
    return url;
  }

  return await result.json();
}

export async function getClientsWithMethods(): Promise<ClientWithMethods[]> {
  const clients = await getClients();

  const final = [];
  for (const client of clients) {
    let methods: ClientMethod[] = [];

    if (client.methodsUrl) {
      methods = await getClientMethods(client.methodsUrl);
    }

    final.push({
      ...client,
      methods: methods,
    });
  }

  return final;
}

export async function getClientMethods(
  methodsUrl: string
): Promise<ClientMethod[]> {
  const result = await GET(methodsUrl);

  if (!result) return [];
  return mapClientMethod(await result.arrayBuffer());
}
