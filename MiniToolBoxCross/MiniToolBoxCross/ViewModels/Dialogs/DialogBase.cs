using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using MiniToolBoxCross.Models.Repositories;

namespace MiniToolBoxCross.ViewModels.Dialogs;

public class DialogBase : ObservableValidator
{
    public INotificationService NotificationService { get; set; } = null!;

    protected bool IsValidation()
    {
        var validationContext = new ValidationContext(this);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(this, validationContext, validationResults, true);

        if (!isValid)
        {
            NotificationService.ShowError(
                "验证错误",
                string.Join(Environment.NewLine, validationResults.Select(x => x.ErrorMessage))
            );
        }

        return isValid;
    }
}
