import { env } from "~/environment";
import { get, post } from "../requests/requests";
import type { Client, ClientName, ClientWithMethods } from "./contracts/client";
import type {
  ClientMethodArgument,
  ClientMethod,
  ClientMethodArgumentComponents,
  ClientMethodArgumentFlags,
} from "./contracts/clientMethods";
import { BinaryReader } from "../utils/binaryReader";

export async function addNewClient(client: ClientName): Promise<Client | null> {
    const result = await post(`${env.API_BASE_URL}/clients`, client);
    return await result?.json();
}

export async function getClients(): Promise<Client[]> {
  const result = await get(`${env.API_BASE_URL}/clients`);

  try {
    const data = (await result?.json()) ?? [];
    return data as Client[];
  } catch (error) {
    console.error("Error parsing JSON response:", error);
  }

  return [];
}

export async function callClientMethod(client: string, method: string, data: any): Promise<any | null> {
  const formattedData: any = [];
  Object.keys(data).forEach(key => {
    formattedData.push(data[key])
  });

  const result = await post(`${env.API_BASE_URL}/websockets/${client}/${method}`, formattedData)
  if (!result) return null;

  const base64Json = await result.text();
  const jsonString = atob(base64Json);

  return JSON.parse(jsonString);
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
  const result = await get(methodsUrl);

  if (!result) return [];

  try {
    const binaryData = await result.arrayBuffer();
    const reader = new BinaryReader(binaryData);
    const methods: ClientMethod[] = [];
    const typeMap = new Map<number, ClientMethodArgumentComponents>();

    const version = reader.nextByte();
    if (version !== 0) throw new Error("Unsupported methods data version");

    const operation = reader.nextByte();
    if (operation !== 1) throw new Error("Unsupported methods data operation");

    const signature = reader.nextBytes(32);

    const methodsCount = reader.nextByte();
    for (let i = 0; i < methodsCount; i++) {
      const methodName = reader.nextString();

      const methodInfo = reader.nextByte();
      const returns = methodInfo & 0x80 ? true : false;
      const argumentCount = methodInfo & 0x7f;

      const methodArgs: ClientMethodArgument[] = [];
      for (let j = 0; j < argumentCount; j++) {
        methodArgs.push(getMethodArgument(reader, typeMap));
      }

      const ret = returns ? getMethodArgument(reader, typeMap) : null;

      methods.push({
        name: methodName,
        arguments: methodArgs,
        returns: ret,
      });
    }

    return methods;
  } catch (error) {
    console.error("Error reading binary data:", error);
    return [];
  }
}

function getMethodArgument(
  reader: BinaryReader,
  typeMap: Map<number, ClientMethodArgumentComponents>
): ClientMethodArgument {
  const argName = reader.nextString();
  const flags = reader.nextByte();

  const isKnownComplexType = (flags & 0x01) != 0;
  const typeFlags: ClientMethodArgumentFlags = {
    isArray: (flags & 0x02) != 0,
    isNullable: (flags & 0x04) != 0,
    isNumeric: (flags & 0x08) != 0,
    isBoolean: (flags & 0x10) != 0,
  };

  const typeComponents = isKnownComplexType
    ? (typeMap.get(reader.nextInt32()) as ClientMethodArgumentComponents)
    : getTypeComponents(reader, typeMap);

  return {
    name: argName,
    ...typeComponents,
    ...typeFlags,
  };
}

function getTypeComponents(
  reader: BinaryReader,
  typeMap: Map<number, ClientMethodArgumentComponents>
): ClientMethodArgumentComponents {
  const newTypeOffset = reader.getOffset();
  const typeComponents: ClientMethodArgument[] = [];

  const componentsCount = reader.nextByte();
  for (let i = 0; i < componentsCount; ++i) {
    typeComponents.push(getMethodArgument(reader, typeMap));
  }

  typeMap.set(newTypeOffset, { components: typeComponents });
  return { components: typeComponents };
}
