import { IconButton, Input, TableCell, TableRow } from "@mui/material";
import CheckIcon from '@mui/icons-material/Check';
import CloseIcon from '@mui/icons-material/Close';
import { useState } from "react";

export interface NewClientRowProps {
  clientKey: number;
  onSave: (key: number, client: string) => void;
  onRemove: (key: number) => void;
}

export function NewClientRow({ clientKey, onSave, onRemove }: NewClientRowProps) {
  const [name, setName] = useState("");
  
  return (
    <TableRow
      key={clientKey}
      sx={{ "&:last-child td, &:last-child th": { border: 0 } }}
    >
      <TableCell component="th" scope="row">
        <Input
          placeholder="Enter client name"
          value={name}
          onChange={(e) => setName(e.target.value)}
        ></Input>
      </TableCell>
      <TableCell align="right"></TableCell>
      <TableCell align="right"></TableCell>
      <TableCell align="right"></TableCell>
      <TableCell align="right">
        <IconButton onClick={() => onSave(clientKey, name)}>
            <CheckIcon/>
        </IconButton>
        <IconButton onClick={() => onRemove(clientKey)}>
            <CloseIcon/>
        </IconButton>
      </TableCell>
    </TableRow>
  );
}
