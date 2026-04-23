import { env } from "~/environment";
import { DELETE, GET, POST, POST_RAW } from "../requests/requests";
import type {
  Client,
  ClientConnectionStatus,
  ClientName,
  ClientTokenData,
  ClientWithMethods,
} from "../../contracts/client";
import type { ClientMethod } from "../../contracts/clientMethods";
import { mapClientMethod } from "~/mappers/clientMethodMapper";

function mapClient(data: any): Client {
  return {
    userId: data.userId,
    name: data.name,
    methodsUrl: data.methodsUrl,
    isConnected: false,
    signature: data.signature,
    tokenHash: data.tokenHash,
    sharedWithUserEmails: data.sharedWithUserEmails ?? [],
  };
}

export async function createClient(client: ClientName): Promise<Client | null> {
  const result = await POST(`${env.API_BASE_URL}/clients`, client);
  if (!result) return null;

  return mapClient(await result.json());
}

export async function getClients(onlyOwned: boolean = false): Promise<Client[]> {
  const result = await GET(`${env.API_BASE_URL}/clients?onlyOwned=${onlyOwned}`);

  try {
    const data = ((await result?.json()) ?? []) as any[];

    const clients = data.map((d) => mapClient(d));

    clients.sort((a, b) => (a.name > b.name ? 1 : -1));

    return clients;
  } catch (error) {
    console.error("Error parsing JSON response:", error);
  }

  return [];
}

export async function getClientsConnectionStatus(): Promise<ClientConnectionStatus[]> {
  const result = await GET(`${env.API_BASE_URL}/clients/connection-status`);

  try {
    const data = ((await result?.json()) ?? []) as any[];

    return data.map((d) => {
      return {
        userId: d.userId,
        name: d.name,
        isConnected: d.isConnected,
      };
    });

  } catch (error) {
    console.error("Error parsing JSON response:", error);
  }

  return [];
}

export async function deleteClient(client: ClientName): Promise<boolean> {
  const response = await DELETE(`${env.API_BASE_URL}/clients/${client.name}`);
  return response != null;
}

export async function addClientShare(client: ClientName, email: string): Promise<boolean> {
  const response = await POST(
    `${env.API_BASE_URL}/clients/${encodeURIComponent(client.name)}/share`,
    {
      sharedWithUserEmail: email,
    }
  );

  return response != null;
}

export async function deleteClientShare(client: ClientName, email: string): Promise<boolean> {
  const response = await DELETE(
    `${env.API_BASE_URL}/clients/${encodeURIComponent(client.name)}/share/${encodeURIComponent(email)}`
  );

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
  userId: string,
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
      `${env.APP_BASE_URL}/client-portal/${userId}/${client}/${method}`,
      data
    );  
  } else {
    if (data) {
      Object.keys(data).forEach((key) => {
        formattedData.push(data[key]);
      });
    }

    result = await POST(
      `${env.APP_BASE_URL}/client-portal/${userId}/${client}/${method}`,
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

export function getClientPortalUrl( userId: string, client: string, method: string): string {
  return `${env.APP_BASE_URL}/client-portal/${userId}/${client}/${method}`;
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
