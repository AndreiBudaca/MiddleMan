import type {
  ClientMethod,
  ClientMethodArgument,
  ClientMethodArgumentComponents,
  ClientMethodArgumentFlags,
} from "~/contracts/clientMethods";
import { BinaryReader } from "~/utils/binaryReader";

export function mapClientMethod(binaryData: ArrayBuffer): ClientMethod[] {
  try {
    const reader = new BinaryReader(binaryData);
    const methods: ClientMethod[] = [];
    const typeMap = new Map<number, ClientMethodArgumentComponents>();

    const version = reader.nextByte();
    if (version !== 0) throw new Error("Unsupported methods data version");

    const operation = reader.nextByte();
    if (operation !== 1) throw new Error("Unsupported methods data operation");

    const signature = reader.nextBytes(32);

    const methodsCount = reader.nextByte();
    for (let i = 0; i < methodsCount; i++) {
      const methodName = reader.nextString();

      const methodInfo = reader.nextByte();
      const returns = methodInfo & 0x80 ? true : false;
      const argumentCount = methodInfo & 0x7f;

      const methodArgs: ClientMethodArgument[] = [];
      for (let j = 0; j < argumentCount; j++) {
        methodArgs.push(getMethodArgument(reader, typeMap));
      }

      const ret = returns ? getMethodArgument(reader, typeMap) : null;

      methods.push({
        name: methodName,
        arguments: methodArgs,
        returns: ret,
      });
    }

    return methods;
  } catch (error) {
    console.error("Error reading binary data:", error);
    return [];
  }
}

function getMethodArgument(
  reader: BinaryReader,
  typeMap: Map<number, ClientMethodArgumentComponents>
): ClientMethodArgument {
  const argName = reader.nextString();
  const flags = reader.nextByte();

  const isKnownComplexType = (flags & 0x01) != 0;
  const typeFlags: ClientMethodArgumentFlags = {
    isArray: (flags & 0x02) != 0,
    isNullable: (flags & 0x04) != 0,
    isNumeric: (flags & 0x08) != 0,
    isBoolean: (flags & 0x10) != 0,
  };

  const typeComponents = isKnownComplexType
    ? (typeMap.get(reader.nextInt32()) as ClientMethodArgumentComponents)
    : getTypeComponents(reader, typeMap);

  return {
    name: argName,
    ...typeComponents,
    ...typeFlags,
  };
}

function getTypeComponents(
  reader: BinaryReader,
  typeMap: Map<number, ClientMethodArgumentComponents>
): ClientMethodArgumentComponents {
  const newTypeOffset = reader.getOffset();
  const typeComponents: ClientMethodArgument[] = [];

  const componentsCount = reader.nextByte();
  for (let i = 0; i < componentsCount; ++i) {
    typeComponents.push(getMethodArgument(reader, typeMap));
  }

  typeMap.set(newTypeOffset, { components: typeComponents });
  return { components: typeComponents };
}
