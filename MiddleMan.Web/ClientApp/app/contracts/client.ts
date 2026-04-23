import type { ClientMethod } from "./clientMethods";

export interface ClientName
{
  name: string;
}

export interface ClientConnectionStatus extends ClientName
{
  userId: string;
  isConnected: boolean;
}

export interface Client extends ClientName {
  userId: string;
  isConnected: boolean;
  methodsUrl: string | null;
  signature: string | null;
  tokenHash: string | null;
  sharedWithUserEmails: string[];
}

export interface ClientWithMethods extends Client {
  methods: ClientMethod[];
}

export interface ClientTokenData {
  token: string | null;
  tokenHash: string | null;
}