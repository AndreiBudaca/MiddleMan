import { useEffect, useState } from "react";
import type {
  ClientConnectionStatus,
  ClientWithMethods
} from "~/contracts/client";
import { getClientsConnectionStatus, getClientsWithMethods } from "~/services/clients/clientService";
import ClientTreeView from "./clientTreeView";
import { Box } from "@mui/material";
import type { ClientMethod } from "~/contracts/clientMethods";
import MethodsView from "./methodsView";

export default function DashboardClients() {
  const [clients, setClients] = useState<ClientWithMethods[]>([]);
  const [clientConnectionStatus, setClientConnectionStatus] = useState<
    ClientConnectionStatus[]
  >([]);
  const [selectedClient, setSelectedClient] =
    useState<ClientWithMethods | null>(null);
  const [selectedMethod, setSelectedMethod] = useState<ClientMethod | null>(
    null,
  );
  const isSelectedClientConnected = clientConnectionStatus.find(
    (s) => s.name == selectedClient?.name,
  )?.isConnected ?? false;

  const onSelectedMethodChange = (path: string) => {
    const pathParts = path.split("/");
    if (pathParts.length != 2) return;

    const eligibleClients = clients.filter((c) => c.name == pathParts[0]);
    if (
      !eligibleClients ||
      !eligibleClients.length ||
      eligibleClients.length < 1
    )
      return;

    const eligibleMethods = eligibleClients[0].methods.filter(
      (m) => m.name == pathParts[1],
    );
    if (
      !eligibleMethods ||
      !eligibleMethods.length ||
      eligibleMethods.length < 1
    )
      return;

    setSelectedClient(eligibleClients[0]);
    setSelectedMethod(eligibleMethods[0]);
  };

  useEffect(() => {
    const fetchClients = async () => {
      const result = await getClientsWithMethods();
      setClients(result);
    };
    fetchClients();
  }, []);

  useEffect(() => {
    const fetchClientConnectionStatus = async () => {
      const result = await getClientsConnectionStatus();
      setClientConnectionStatus(result);
    };

    if (clients && clients.length > 0) {
      fetchClientConnectionStatus();
    }
  }, [clients]);

  return (
    <Box
      marginTop="50px"
      display="flex"
      width="100%"
      gap="20px"
      flexWrap="wrap"
    >
      <ClientTreeView
        clients={clients}
        clientsConnectionStatus={clientConnectionStatus}
        onNodeSelected={onSelectedMethodChange}
      />
      <MethodsView
        client={selectedClient?.name}
        method={selectedMethod}
        isCallable={isSelectedClientConnected}
      />
    </Box>
  );
}
