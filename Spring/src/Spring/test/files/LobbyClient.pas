begin
  Result :=
  {$IFDEF MSWINDOWS}
    0
  {$ELSE}
    {$IFDEF LINUX}
    1
    {$ELSE}
      {$IFDEF DARWIN}
    2
      {$ELSE}
    -1
      {$ENDIF}
    {$ENDIF}
  {$ENDIF}
  ;
end;

var
  Json: TJSONOBject;
  JsonPlayers: TJSONArray;
  i: Integer;
begin
  inherited Create(False);
  Debug('[Lobby] Registering the server in lobby');
  FURL := sv_lobbyurl.Value + '/v0/servers/register';

  Json := TJSONObject.Create;

  Json.Add('AC', {$IFDEF ENABLE_FAE}ac_enable.Value{$ELSE}False{$ENDIF});
  Json.Add('AuthMode', 0);
  Json.Add('Advanced', sv_advancemode.Value);
  Json.Add('BonusFreq', sv_bonus_frequency.Value);
  Json.Add('ConnectionType', 0);
  Json.Add('CurrentMap', Map.Name);
  Json.Add('GameStyle', sv_gamemode.Value);
  Json.Add('Info', sv_info.Value);
  Json.Add('MaxPlayers', sv_maxplayers.Value);
  Json.Add('Modded', fs_mod.Value <> '');
  Json.Add('Name', sv_hostname.Value);
  Json.Add('NumBots', BotsNum);
  Json.Add('NumPlayers', PlayersNum);
  Json.Add('OS', GetOS);
  Json.Add('Port', net_port.Value);
  Json.Add('Private', sv_password.Value <> '');
  Json.Add('Realistic', sv_realisticmode.Value);
  Json.Add('Respawn', sv_respawntime.Value);
  Json.Add('Survival', sv_survivalmode.Value);
  Json.Add('Version', SOLDAT_VERSION);
  Json.Add('WM', LoadedWMChecksum <> DefaultWMChecksum);

  JsonPlayers := TJsonArray.Create;
  for i := 1 to MAX_PLAYERS do
    if (Sprite[i].Active) then
      JsonPlayers.Add(Sprite[i].Player.Name);

  Json.Add('Players', JsonPlayers);

  FData := TStringStream.Create(Json.AsJson);

  FreeOnTerminate := True;
end;

procedure TLobbyThread.Execute;
begin
  Client := TFPHTTPClient.Create(Nil);
  with Client do
  try
    try
      AddHeader('User-Agent', 'soldatserver/' + SOLDAT_VERSION);
      AddHeader('Content-Type', 'application/json');
      AllowRedirect := False;
      RequestBody := FData;
      Post(FURL);
      if ResponseStatusCode <> 204 then
        raise Exception.Create('Wrong response status code ' + IntToStr(ResponseStatusCode));
    except
      on E: Exception do
      begin
        Debug('[Lobby] Lobby register has failed: ' + E.Message);
      end;
    end;
    finally
      Client.Terminate;
    end;
end;

destructor TLobbyThread.Destroy;
begin
  Client.Free;
  inherited Destroy;
end;

end.
