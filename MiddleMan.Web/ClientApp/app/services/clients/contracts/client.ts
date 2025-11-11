import type { ClientMethod } from "./clientMethods";

export interface Client {
  name: string;
  methodsUrl: string | null;
  isConnected: boolean;
}

export interface ClientWithMethods extends Client {
  methods: ClientMethod[];
}
