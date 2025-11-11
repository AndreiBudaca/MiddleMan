export interface ClientMethod {
  name: string;
  arguments: ClientMethodArgument[];
  returns: ClientMethodArgument | null;
}

export interface ClientMethodArgument extends ClientMethodArgumentComponents, ClientMethodArgumentFlags {
  name: string | null;
}

export interface ClientMethodArgumentFlags {
  isArray: boolean;
  isNullable: boolean;
  isNumeric: boolean;
  isBoolean: boolean;
}

export interface ClientMethodArgumentComponents {
  components: ClientMethodArgument[] | null;
}
