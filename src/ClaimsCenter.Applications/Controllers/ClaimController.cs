﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Waf.Applications;
using System.Windows.Input;
using Autofac.Features.OwnedInstances;
using NRules.Samples.ClaimsCenter.Applications.ViewModels;
using NRules.Samples.ClaimsExpert.Contract;

namespace NRules.Samples.ClaimsCenter.Applications.Controllers;

public interface IClaimController
{
    void Initialize();
    void Refresh();
    void RefreshSelected();
}

internal class ClaimController : IClaimController
{
    private readonly Func<Owned<ClaimService.ClaimServiceClient>> _claimServiceFactory;
    private readonly Lazy<MainViewModel> _mainViewModel;
    private readonly Lazy<ClaimListViewModel> _claimListViewModel;
    private readonly Lazy<ClaimViewModel> _claimViewModel;
    private readonly ICommand _refreshCommand;

    public ClaimController(Func<Owned<ClaimService.ClaimServiceClient>> claimServiceFactory, 
        Lazy<MainViewModel> mainViewModel, Lazy<ClaimListViewModel> claimListViewModel, Lazy<ClaimViewModel> claimViewModel)
    {
        _claimServiceFactory = claimServiceFactory;
        _mainViewModel = mainViewModel;
        _claimListViewModel = claimListViewModel;
        _claimViewModel = claimViewModel;
        _refreshCommand = new DelegateCommand(Refresh);
    }

    public void Initialize()
    {
        _mainViewModel.Value.RefreshCommand = _refreshCommand;
        PropertyChangedEventManager.AddHandler(
            _claimListViewModel.Value, 
            (sender, args) => _claimViewModel.Value.Claim = _claimListViewModel.Value.SelectedClaim, 
            "SelectedClaim");
    }

    public void Refresh()
    {
        using var claimService = _claimServiceFactory();
        var response = claimService.Value.GetAll(new GetAllClaimsRequest());
        var claims = new List<ClaimDto>();
        while (response.ResponseStream.MoveNext(CancellationToken.None).Result)
        {
            claims.Add(response.ResponseStream.Current);
        }
        _claimListViewModel.Value.Claims = new ObservableCollection<ClaimDto>(claims);
        _claimViewModel.Value.Claim = null;
    }

    public void RefreshSelected()
    {
        if (_claimListViewModel.Value.SelectedClaim == null) return;
        var selectedClaim = _claimListViewModel.Value.SelectedClaim;
        int claimIndex = _claimListViewModel.Value.Claims.IndexOf(selectedClaim);
        using var claimService = _claimServiceFactory();
        var claim = claimService.Value.FindByClaimId(new FindByClaimIdRequest {ClaimId = selectedClaim.Id});
        _claimListViewModel.Value.Claims[claimIndex] = claim;
        _claimListViewModel.Value.SelectedClaim = claim;
    }
}