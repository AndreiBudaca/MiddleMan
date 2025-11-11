import type {
  ClientMethod,
  ClientMethodArgument,
} from "~/services/clients/contracts/clientMethods";

export function mapToJsonTemplate(method: ClientMethod): any {
  return getObject(method.arguments);
}

function getJsonValue(argument: ClientMethodArgument): any {
  if (argument.isArray) {
    return getArray(argument);
  }

  if (argument.components && argument.components.length > 0) {
    return getObject(argument.components);
  }

  return getPrimitive(argument);
}

function getArray(argument: ClientMethodArgument): any[] {
  const value = [];
  value.push(getJsonValue({ ...argument, isArray: false }));
  return value;
}

function getObject(components: ClientMethodArgument[]): any {
  const template: any = {};
  components
    .filter((arg) => arg.name)
    .forEach((arg) => {
      template[arg.name as string] = getJsonValue(arg);
    });

  return template;
}

function getPrimitive(argument: ClientMethodArgument): any {
  if (argument.isBoolean) return false;
  if (argument.isNumeric) return 0;
  return "";
}
