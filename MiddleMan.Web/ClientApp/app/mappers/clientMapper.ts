import type { TreeViewBaseItem } from "@mui/x-tree-view";
import type { ClientConnectionStatus, ClientWithMethods } from "~/contracts/client";

export interface TreeNode {
  label: string;
  labelSufix: string | null;
  children?: TreeNode[];
}

export function mapToTreeNodes(
  clients: ClientWithMethods[],
  clientsConnectionStatus: ClientConnectionStatus[]
): TreeViewBaseItem[] {
  const mapNode = (
    node: TreeNode,
    selector: string | null = null
  ): TreeViewBaseItem => {
    const currentSelector = selector ? `${selector}/${node.label}` : node.label;
    return {
      id: currentSelector,
      label: node.labelSufix ? `${node.label} - ${node.labelSufix}` : node.label,
      children: node.children?.map((c) => mapNode(c, currentSelector)),
    };
  };

  return clients
    .map((c) => {
      const connectionStatus = clientsConnectionStatus.find(
        (s) => s.name === c.name && s.userId === c.userId
      );
      const isConnected = connectionStatus ? connectionStatus.isConnected : false;
      
      return {
        label: c.name,
        labelSufix: isConnected ? "online" : "offline",
        children: c.methods.map((m) => {
          return {
            label: m.name,
            labelSufix: null,
          };
        }),
      };
    })
    .map((node) => mapNode(node));
}
