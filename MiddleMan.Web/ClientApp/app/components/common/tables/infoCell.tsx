import { TableCell, Typography } from "@mui/material";

export interface InfoCellProps {
    info: string;
}

export function InfoCell({ info }: InfoCellProps) {
  return (
    <TableCell>
      <Typography variant="body1">{info}</Typography>
    </TableCell>
  );
}
