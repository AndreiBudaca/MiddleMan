import type { TreeViewBaseItem } from "@mui/x-tree-view";
import type { ClientWithMethods } from "~/services/clients/contracts/client";

export interface TreeNode {
  label: string;
  labelSufix: string | null;
  children?: TreeNode[];
}

export function mapToTreeNodes(
  clients: ClientWithMethods[]
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
      return {
        label: c.name,
        labelSufix: c.isConnected ? "online" : "offline",
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
