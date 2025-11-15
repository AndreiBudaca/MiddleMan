import {
  Box,
  IconButton,
  TableCell,
  TableRow,
} from "@mui/material";
import type { Client } from "~/contracts/client";
import DeleteIcon from "@mui/icons-material/Delete";
import VpnKeyIcon from "@mui/icons-material/VpnKey";
import KeyOffIcon from "@mui/icons-material/KeyOff";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import { useEffect, useState } from "react";
import { InfoCell } from "~/components/common/tables/infoCell";
import { TrimmedText } from "~/components/common/text/trimmedText";
import { deleteClientToken, refreshClientToken } from "~/services/clients/clientService";

export interface ClientRowProps {
  client: Client;
  onDelete: (name: string) => void;
}

export function ClientRow({ client, onDelete }: ClientRowProps) {
  const [token, setToken] = useState<string | null>(null);
  const [tokenHash, setTokenHash] = useState(client.tokenHash);

  useEffect(() => {
    setToken("");
    setTokenHash(client.tokenHash);
  }, [client]);

  const recreateToken = async () => {
    const tokenData = await refreshClientToken({ name: client.name });
    if (!tokenData) return;

    setToken(tokenData.token);
    setTokenHash(tokenData.tokenHash);
  };

  const removeToken = async () => {
    if (!tokenHash) return;
    const tokenData = await deleteClientToken({ name: client.name });
    if (!tokenData) return;

    setToken(tokenData.token);
    setTokenHash(tokenData.tokenHash);
  };

  const coppyToken = () => {
    if (!token) return;
    navigator.clipboard.writeText(token);
  }

  return (
    <TableRow
      key={client.name}
      sx={{ "&:last-child td, &:last-child th": { border: 0 } }}
    >
      <InfoCell info={client.name} />
      <InfoCell info={client.isConnected ? "online" : "offline"} />
      <InfoCell
        info={client.lastConnectedAt?.toLocaleTimeString() ?? "never"}
      />
      <TableCell>
        <TrimmedText
          text={client.signature ?? "not registered"}
          maxLength={20}
        />
      </TableCell>
      <TableCell>
        <Box
          display="flex"
          alignContent="center"
          justifyContent="space-between"
          gap="5px"
        >
          <Box display="flex" alignItems="center">
            <TrimmedText
              text={tokenHash ?? "not registered"}
              maxLength={20}
            />
          </Box>
          {token && (
            <IconButton onClick={coppyToken}>
              <ContentCopyIcon />
            </IconButton>
          )}
        </Box>
      </TableCell>
      <TableCell>
        <IconButton onClick={removeToken}>
          <KeyOffIcon />
        </IconButton>
        <IconButton onClick={recreateToken}>
          <VpnKeyIcon />
        </IconButton>
        <IconButton onClick={() => onDelete(client.name)}>
          <DeleteIcon />
        </IconButton>
      </TableCell>
    </TableRow>
  );
}
