import type { ClientMethod } from "./clientMethods";

export interface ClientName
{
  name: string;
}

export interface Client extends ClientName {
  methodsUrl: string | null;
  isConnected: boolean;
  lastConnectedAt: Date | null;
  signature: string | null;
  tokenHash: string | null;
}

export interface ClientWithMethods extends Client {
  methods: ClientMethod[];
}

export interface ClientTokenData {
  token: string | null;
  tokenHash: string | null;
}