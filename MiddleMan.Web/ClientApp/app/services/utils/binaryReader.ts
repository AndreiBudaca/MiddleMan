export class BinaryReader {
  private dataView: DataView;
  private offset: number;

  constructor(arrayBuffer: ArrayBuffer) {
    this.dataView = new DataView(arrayBuffer);
    this.offset = 0;
  }

  public getOffset(): number {
    return this.offset;
  }

  public nextByte(): number {
    const value = this.dataView.getUint8(this.offset);
    this.offset += 1;
    return value;
  }

  public nextInt32(): number {
    const value = this.dataView.getUint32(this.offset, true);
    this.offset += 4;
    return value;
  }

  public nextString(): string {
    const bytes: number[] = [];
    let currByte: number;

    do {
      currByte = this.nextByte();
      if (currByte != 0) bytes.push(currByte);
    } while (currByte != 0);

    return String.fromCharCode(...bytes);
  }

  public nextBytes(count: number): number[] {
    const bytes: number[] = [];

    while (count > 0) {
      --count;
      bytes.push(this.nextByte())
    }

    this.offset += count;
    return bytes;
  }
}
