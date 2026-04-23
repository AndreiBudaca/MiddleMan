import {
  Box,
  Input,
  IconButton,
  TableCell,
  TableRow,
  Typography,
} from "@mui/material";
import type { Client } from "~/contracts/client";
import DeleteIcon from "@mui/icons-material/Delete";
import VpnKeyIcon from "@mui/icons-material/VpnKey";
import KeyOffIcon from "@mui/icons-material/KeyOff";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import AddIcon from "@mui/icons-material/Add";
import { useEffect, useState } from "react";
import { InfoCell } from "~/components/common/tables/infoCell";
import { TrimmedText } from "~/components/common/text/trimmedText";
import {
  addClientShare,
  deleteClientShare,
  deleteClientToken,
  refreshClientToken,
} from "~/services/clients/clientService";

export interface ClientRowProps {
  client: Client;
  onDelete: (name: string) => void;
}

export function ClientRow({ client, onDelete }: ClientRowProps) {
  const [token, setToken] = useState<string | null>(null);
  const [tokenHash, setTokenHash] = useState(client.tokenHash);
  const [shareEmail, setShareEmail] = useState("");
  const [sharedWithEmails, setSharedWithEmails] = useState<string[]>(
    client.sharedWithUserEmails
  );

  useEffect(() => {
    setToken("");
    setTokenHash(client.tokenHash);
    setShareEmail("");
    setSharedWithEmails(client.sharedWithUserEmails);
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
  };

  const addShare = async () => {
    const email = shareEmail.trim();
    if (!email) return;

    const alreadyExists = sharedWithEmails.some(
      (e) => e.toLowerCase() === email.toLowerCase()
    );
    if (alreadyExists) return;

    const success = await addClientShare({ name: client.name }, email);
    if (!success) return;

    setSharedWithEmails([...sharedWithEmails, email]);
    setShareEmail("");
  };

  const removeShare = async (email: string) => {
    const success = await deleteClientShare({ name: client.name }, email);
    if (!success) return;

    setSharedWithEmails(sharedWithEmails.filter((e) => e !== email));
  };

  return (
    <TableRow
      key={client.name}
      sx={{ "&:last-child td, &:last-child th": { border: 0 } }}
    >
      <InfoCell info={client.name} />
      <InfoCell info={client.isConnected ? "online" : "offline"} />
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
        <Box display="flex" flexDirection="column" gap="6px" minWidth="230px">
          <Box display="flex" gap="5px" alignItems="center">
            <Input
              placeholder="Share with email"
              value={shareEmail}
              onChange={(e) => setShareEmail(e.target.value)}
              fullWidth
            />
            <IconButton onClick={addShare}>
              <AddIcon />
            </IconButton>
          </Box>

          {sharedWithEmails.length === 0 && (
            <Typography variant="body2" color="text.secondary">
              no shares
            </Typography>
          )}

          {sharedWithEmails.map((email) => (
            <Box
              key={email}
              display="flex"
              justifyContent="space-between"
              alignItems="center"
              gap="5px"
            >
              <TrimmedText text={email} maxLength={26} />
              <IconButton onClick={() => removeShare(email)}>
                <DeleteIcon />
              </IconButton>
            </Box>
          ))}
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
